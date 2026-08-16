using System;
using TrayShot.Infrastructure;
using Xunit;

namespace TrayShot.Tests.Infrastructure;

public class ThemeManagerTests
{
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
            settings.Theme = initialTheme;
        }
    }
}
