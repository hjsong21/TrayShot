using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using TrayShot.Infrastructure;

namespace TrayShot.Tests;

public sealed class TestSettingsScope : IDisposable
{
    private readonly string _originalPath;
    private readonly string _backupJson;
    private readonly string _tempPath;

    public string TempPath => _tempPath;

    public TestSettingsScope()
    {
        var settings = AppSettings.Shared;
        var pathField = typeof(AppSettings).GetField("_settingsFilePath", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var dataField = typeof(AppSettings).GetField("_data", BindingFlags.NonPublic | BindingFlags.Instance)!;

        _originalPath = (string)pathField.GetValue(settings)!;
        object originalData = dataField.GetValue(settings)!;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
        _backupJson = JsonSerializer.Serialize(originalData, options);

        _tempPath = Path.Combine(Path.GetTempPath(), $"trayshot_test_settings_{Guid.NewGuid():N}.json");
        File.WriteAllText(_tempPath, _backupJson);

        pathField.SetValue(settings, _tempPath);
    }

    public void Dispose()
    {
        var settings = AppSettings.Shared;
        var pathField = typeof(AppSettings).GetField("_settingsFilePath", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var dataField = typeof(AppSettings).GetField("_data", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Restore original path
        pathField.SetValue(settings, _originalPath);

        // Restore original data
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
        Type dataType = dataField.FieldType;
        object? restoredData = JsonSerializer.Deserialize(_backupJson, dataType, options);
        if (restoredData != null)
        {
            dataField.SetValue(settings, restoredData);
        }

        if (File.Exists(_tempPath))
        {
            try { File.Delete(_tempPath); } catch { }
        }
    }
}
