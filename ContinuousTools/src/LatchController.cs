using System;
using System.Diagnostics;

namespace ContinuousTools;

internal enum ToolKind { Drill, Welder, Grinder }

/// <summary>
/// Edge-driven latch. Native presses are never delayed. The second press inside the configured
/// window latches by retaining the game's native active state and suppressing only that release.
/// Every external interruption calls the captured native release path before state is forgotten.
/// </summary>
internal sealed class LatchController
{
    private readonly Settings _settings;
    private object? _tool;
    private ToolKind _kind;
    private Action<bool>? _native;
    private long _lastPress;
    private bool _latched;
    private bool _suppressUntilRelease;

    internal LatchController(Settings settings) => _settings = settings;

    internal bool IsCurrent(object tool) => ReferenceEquals(_tool, tool);

    internal void Input(object tool, ToolKind kind, bool pressed, Action<bool> native)
    {
        try
        {
            if (!_settings.Enabled || !Enabled(kind))
            {
                if (IsCurrent(tool)) Stop("tool disabled in settings");
                native(pressed);
                return;
            }

            if (_tool != null && !ReferenceEquals(_tool, tool)) Stop("equipped tool changed");
            _tool = tool;
            _kind = kind;
            _native = native;

            if (!pressed)
            {
                if (_suppressUntilRelease)
                {
                    _suppressUntilRelease = false;
                    native(false);
                    return;
                }

                if (_latched) return;
                native(false);
                return;
            }

            if (_latched)
            {
                native(false);
                Log.Write($"latch OFF tool={kind} reason=single click");
                Clear(keepSuppression: true);
                return;
            }

            var now = Stopwatch.GetTimestamp();
            var elapsedMs = _lastPress == 0
                ? double.MaxValue
                : (now - _lastPress) * 1000d / Stopwatch.Frequency;

            native(true); // First press and second press both retain native immediate behavior.
            if (elapsedMs <= _settings.DoubleClickWindowMs)
            {
                _latched = true;
                _lastPress = 0;
                Log.Write($"latch ON tool={kind} intervalMs={elapsedMs:F0}");
            }
            else
            {
                _lastPress = now;
                if (_settings.DiagnosticLogging) Log.Write($"first click tool={kind}");
            }
        }
        catch (Exception ex)
        {
            Log.Error("input state failure; forcing release", ex);
            Stop("exception");
            try { native(false); } catch { }
        }
    }

    internal void Stop(string reason)
    {
        var wasLatched = _latched;
        var native = _native;
        var kind = _kind;
        Clear(keepSuppression: false);

        try { native?.Invoke(false); }
        catch (Exception ex) { Log.Error("native release failed", ex); }

        if (wasLatched) Log.Write($"latch OFF tool={kind} reason={reason}");
        else if (_settings.DiagnosticLogging) Log.Write($"state reset reason={reason}");
    }

    private void Clear(bool keepSuppression)
    {
        _tool = null;
        _native = null;
        _lastPress = 0;
        _latched = false;
        _suppressUntilRelease = keepSuppression;
    }

    private bool Enabled(ToolKind kind) => kind switch
    {
        ToolKind.Drill => _settings.EnableDrill,
        ToolKind.Welder => _settings.EnableWelder,
        ToolKind.Grinder => _settings.EnableGrinder,
        _ => false
    };
}
