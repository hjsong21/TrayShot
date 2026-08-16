using System;
using System.IO;
using TrayShot.Infrastructure;
using Xunit;

namespace TrayShot.Tests.Infrastructure;

public class AppSettingsTests : IDisposable
{
    private readonly string _tempSettingsPath;

    public AppSettingsTests()
    {
        _tempSettingsPath = Path.Combine(Path.GetTempPath(), $"trayshot_settings_{Guid.NewGuid():N}.json");
    }

    [Fact]
    public void AppSettings_SavesAndLoadsSettingsCorrectly()
    {
        var settings = new AppSettings(_tempSettingsPath);
        settings.OcrEnabled = false;
        settings.AddFolder(@"C:\TestScreenshots");

        var loadedSettings = new AppSettings(_tempSettingsPath);

        Assert.False(loadedSettings.OcrEnabled);
        Assert.Contains(@"C:\TestScreenshots", loadedSettings.Folders);
    }

    [Fact]
    public void AppSettings_TriggersEventOnPropertyChanged()
    {
        var settings = new AppSettings(_tempSettingsPath);
        bool eventFired = false;
        settings.OcrEnabledChanged += () => eventFired = true;

        settings.OcrEnabled = !settings.OcrEnabled;

        Assert.True(eventFired);
    }

    public void Dispose()
    {
        if (File.Exists(_tempSettingsPath))
        {
            try { File.Delete(_tempSettingsPath); } catch { }
        }
    }
}
