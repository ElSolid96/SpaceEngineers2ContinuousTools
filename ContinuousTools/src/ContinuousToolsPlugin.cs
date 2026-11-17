using System;
using System.Reflection;
using Keen.VRage.Core.Plugins;

namespace ContinuousTools;

public sealed class ContinuousToolsPlugin : IPlugin, IDisposable
{
    private static ContinuousToolsInstaller? _installer;

    public ContinuousToolsPlugin() => Log.Write($"ContinuousTools {Version()} constructing");

    public ContinuousToolsPlugin(PluginHost host) : this()
    {
        try
        {
            var settings = Settings.Load();
            Log.Write($"settings enabled={settings.Enabled} windowMs={settings.DoubleClickWindowMs}"
                      + $" drill={settings.EnableDrill} welder={settings.EnableWelder} grinder={settings.EnableGrinder}");
            _installer = new ContinuousToolsInstaller(settings);
            _installer.Install();
            Log.Write("ContinuousTools ready (in-game validation required)");
        }
        catch (Exception ex)
        {
            Log.Error("initialization failed", ex);
        }
    }

    private static string Version()
        => typeof(ContinuousToolsPlugin).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(ContinuousToolsPlugin).Assembly.GetName().Version?.ToString()
            ?? "unknown";

    public void Dispose()
    {
        _installer?.Dispose();
        _installer = null;
    }
}
