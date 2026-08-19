using System;
using System.IO;
using TrayShot.Infrastructure;
using Xunit;

namespace TrayShot.Tests.Infrastructure;

public class ThemeManagerTests : IDisposable
{
    private readonly TestSettingsScope _settingsScope;

    public ThemeManagerTests()
    {
        _settingsScope = new TestSettingsScope();
    }

    [Fact]
    public void AppSettings_ChangesThemePropertyCorrectly()
    {
        var settings = AppSettings.Shared;
        var initialTheme = settings.Theme;
        var targetTheme = initialTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;

        bool eventFired = false;
        Action handler = () => eventFired = true;
        settings.ThemeChanged += handler;

        try
        {
            settings.Theme = targetTheme;
            Assert.Equal(targetTheme, settings.Theme);
            Assert.True(eventFired);
        }
        finally
        {
            settings.ThemeChanged -= handler;
        }
    }

    public void Dispose()
    {
        _settingsScope.Dispose();
    }
}
