using System;
using System.Diagnostics;
using System.IO;

namespace Sukurini.Infrastructure;

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}

public sealed class CategoryLogger
{
    private readonly string _category;

    public CategoryLogger(string category)
    {
        _category = category;
    }

    public void Debug(string message) => LogInternal(LogLevel.Debug, message);
    public void Info(string message) => LogInternal(LogLevel.Info, message);
    public void Warn(string message) => LogInternal(LogLevel.Warn, message);
    public void Error(string message) => LogInternal(LogLevel.Error, message);

    private void LogInternal(LogLevel level, string message)
    {
        string formatted = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [{_category}] {message}";
        Trace.WriteLine(formatted);
        Console.WriteLine(formatted);
        Log.AppendToFile(formatted);
    }
}

public static class Log
{
    private static readonly object _lock = new();
    private static string? _logFilePath;

    public static CategoryLogger App { get; } = new("App");
    public static CategoryLogger Store { get; } = new("Store");
    public static CategoryLogger Watcher { get; } = new("Watcher");
    public static CategoryLogger Gallery { get; } = new("Gallery");
    public static CategoryLogger Ocr { get; } = new("Ocr");
    public static CategoryLogger Convert { get; } = new("Convert");
    public static CategoryLogger Search { get; } = new("Search");
    public static CategoryLogger Semantic { get; } = new("Semantic");
    public static CategoryLogger Organize { get; } = new("Organize");
    public static CategoryLogger Settings { get; } = new("Settings");
    public static CategoryLogger Update { get; } = new("Update");
    public static CategoryLogger Telemetry { get; } = new("Telemetry");

    public static void Initialize(string? customLogDir = null)
    {
        string logDir = customLogDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Sukurini",
            "logs");

        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        _logFilePath = Path.Combine(logDir, $"sukurini_{DateTime.Now:yyyyMMdd}.log");
    }

    internal static void AppendToFile(string line)
    {
        if (_logFilePath == null) Initialize();

        lock (_lock)
        {
            try
            {
                File.AppendAllLines(_logFilePath!, new[] { line });
            }
            catch
            {
                // Ignore log writing exceptions to prevent crash
            }
        }
    }
}
