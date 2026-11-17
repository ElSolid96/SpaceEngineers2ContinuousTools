using System;
using System.Reflection;
using Avalonia.Controls;
using Keen.Game2.Client.Input;
using Keen.Game2.Simulation.WorldObjects.Tools;
using Keen.Game2.Simulation.WorldObjects.Tools.Voxels;
using Keen.VRage.UI.Screens;
using MonoMod.RuntimeDetour;

namespace ContinuousTools;

internal sealed class ContinuousToolsInstaller : IDisposable
{
    private readonly LatchController _latch;
    private readonly Settings _settings;
    private Hook? _drillInput;
    private Hook? _integrityInput;
    private Hook? _drillRemoved;
    private Hook? _integrityRemoved;
    private Hook? _screenOpened;
    private Hook? _focusLost;
    private Hook? _inputDestroyed;

    private static ContinuousToolsInstaller? Current;

    internal ContinuousToolsInstaller(Settings settings)
    {
        _settings = settings;
        _latch = new LatchController(settings);
    }

    internal void Install()
    {
        Current = this;
        _drillInput = HookRequired(typeof(DrillToolComponent), "RequestPrimaryAction",
            new[] { typeof(bool) }, (DrillInputHook)OnDrillInput);
        _integrityInput = HookRequired(typeof(IntegrityHandToolComponent),
            "RequestActivatePrimaryActions", new[] { typeof(bool) }, (IntegrityInputHook)OnIntegrityInput);

        _drillRemoved = HookRequired(typeof(DrillToolComponent), "OnRemovedFromScene",
            Type.EmptyTypes, (DrillRemovedHook)OnDrillRemoved);
        _integrityRemoved = HookRequired(typeof(IntegrityHandToolComponent), "OnBeforeRemoved",
            Type.EmptyTypes, (IntegrityRemovedHook)OnIntegrityRemoved);

        _screenOpened = HookRequired(typeof(ScreenManager), "FinalizeOpen",
            new[] { typeof(ScreenView) }, (ScreenOpenedHook)OnScreenOpened);
        _focusLost = HookRequired(typeof(WindowBase), "HandleDeactivated",
            Type.EmptyTypes, (FocusLostHook)OnFocusLost);
        _inputDestroyed = HookRequired(typeof(InputGameComponent), "Destroy",
            Type.EmptyTypes, (InputDestroyedHook)OnInputDestroyed);

        Log.Write("hooks installed: drill input, integrity input, tool removal, screen open, focus loss, input teardown");
    }

    public void Dispose()
    {
        _latch.Stop("plugin unload");
        _inputDestroyed?.Dispose();
        _focusLost?.Dispose();
        _screenOpened?.Dispose();
        _integrityRemoved?.Dispose();
        _drillRemoved?.Dispose();
        _integrityInput?.Dispose();
        _drillInput?.Dispose();
        Current = null;
        Log.Write("hooks removed");
    }

    private static Hook HookRequired(Type type, string name, Type[] parameters, Delegate replacement)
    {
        var method = type.GetMethod(name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, parameters, null)
            ?? throw new MissingMethodException(type.FullName, name);
        return new Hook(method, replacement);
    }

    private delegate void OriginalDrillInput(DrillToolComponent self, bool enabled);
    private delegate void DrillInputHook(OriginalDrillInput original, DrillToolComponent self, bool enabled);
    private static void OnDrillInput(OriginalDrillInput original, DrillToolComponent self, bool enabled)
        => Current!._latch.Input(self, ToolKind.Drill, enabled, value => original(self, value));

    private delegate void OriginalIntegrityInput(IntegrityHandToolComponent self, bool enabled);
    private delegate void IntegrityInputHook(OriginalIntegrityInput original, IntegrityHandToolComponent self, bool enabled);
    private static void OnIntegrityInput(OriginalIntegrityInput original, IntegrityHandToolComponent self, bool enabled)
    {
        var kind = ResolveIntegrityKind(self);
        Current!._latch.Input(self, kind, enabled, value => original(self, value));
    }

    // Verified enum values: Component is welding, Raw is grinding.
    private static ToolKind ResolveIntegrityKind(IntegrityHandToolComponent tool)
    {
        var property = typeof(IntegrityHandToolComponent).GetProperty(
            "CurrentMode", BindingFlags.Instance | BindingFlags.NonPublic);
        var mode = property?.GetValue(tool)?.ToString();
        return string.Equals(mode, "Raw", StringComparison.Ordinal) ? ToolKind.Grinder : ToolKind.Welder;
    }

    private delegate void OriginalDrillRemoved(DrillToolComponent self);
    private delegate void DrillRemovedHook(OriginalDrillRemoved original, DrillToolComponent self);
    private static void OnDrillRemoved(OriginalDrillRemoved original, DrillToolComponent self)
    {
        if (Current!._latch.IsCurrent(self)) Current._latch.Stop("drill removed or unequipped");
        original(self);
    }

    private delegate void OriginalIntegrityRemoved(IntegrityHandToolComponent self);
    private delegate void IntegrityRemovedHook(OriginalIntegrityRemoved original, IntegrityHandToolComponent self);
    private static void OnIntegrityRemoved(OriginalIntegrityRemoved original, IntegrityHandToolComponent self)
    {
        if (Current!._latch.IsCurrent(self)) Current._latch.Stop("integrity tool removed or unequipped");
        original(self);
    }

    private delegate void OriginalScreenOpened(ScreenManager self, ScreenView screen);
    private delegate void ScreenOpenedHook(OriginalScreenOpened original, ScreenManager self, ScreenView screen);
    private static void OnScreenOpened(OriginalScreenOpened original, ScreenManager self, ScreenView screen)
    {
        var screenName = screen.GetType().Name;
        if (ShouldStopForScreen(screenName))
            Current!._latch.Stop($"screen opened: {screenName}");
        else if (Current!._settings.DiagnosticLogging)
            Log.Write($"ignored transient tool screen: {screenName}");
        original(self, screen);
    }

    // These are in-world HUD overlays raised by normal welder/grinder interaction. Treating them
    // as menus interrupts both native holds and the second click that establishes the latch.
    internal static bool ShouldStopForScreen(string screenName)
        => !string.Equals(screenName, "BlockIntegrityScreen", StringComparison.Ordinal)
           && !string.Equals(screenName, "InteractionHintScreen", StringComparison.Ordinal);

    private delegate void OriginalFocusLost(WindowBase self);
    private delegate void FocusLostHook(OriginalFocusLost original, WindowBase self);
    private static void OnFocusLost(OriginalFocusLost original, WindowBase self)
    {
        Current!._latch.Stop("game focus lost");
        original(self);
    }

    private delegate void OriginalInputDestroyed(InputGameComponent self);
    private delegate void InputDestroyedHook(OriginalInputDestroyed original, InputGameComponent self);
    private static void OnInputDestroyed(OriginalInputDestroyed original, InputGameComponent self)
    {
        Current!._latch.Stop("world/input teardown");
        original(self);
    }
}
