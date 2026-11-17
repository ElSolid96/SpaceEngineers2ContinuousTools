using System;
using System.IO;
using System.Text.Json;

namespace ContinuousTools;

internal sealed class Settings
{
    internal const int MinimumWindowMs = 150;
    internal const int MaximumWindowMs = 1000;

    public bool Enabled { get; set; } = true;
    public int DoubleClickWindowMs { get; set; } = 300;
    public bool EnableDrill { get; set; } = true;
    public bool EnableWelder { get; set; } = true;
    public bool EnableGrinder { get; set; } = true;
    public bool DiagnosticLogging { get; set; }

    internal static Settings Load()
    {
        var path = Path.Combine(Log.Folder, "settings.json");
        try
        {
            Directory.CreateDirectory(Log.Folder);
            Settings value;
            if (File.Exists(path))
                value = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new Settings();
            else
                value = new Settings();

            value.DoubleClickWindowMs = Math.Clamp(
                value.DoubleClickWindowMs, MinimumWindowMs, MaximumWindowMs);
            File.WriteAllText(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
            return value;
        }
        catch (Exception ex)
        {
            Log.Error("settings load failed; using defaults", ex);
            return new Settings();
        }
    }
}
