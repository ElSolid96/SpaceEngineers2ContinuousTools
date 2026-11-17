using System;
using System.IO;

namespace ContinuousTools;

internal static class Log
{
    private static readonly object Gate = new();
    internal static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpaceEngineers2", "ContinuousTools");
    internal static readonly string PathName = Path.Combine(Folder, "ContinuousTools.log");

    internal static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Folder);
                File.AppendAllText(PathName, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
            }
        }
        catch { /* Logging must never break tool input. */ }
    }

    internal static void Error(string message, Exception exception)
        => Write($"ERROR {message}: {exception}");
}
