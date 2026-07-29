using Sukurini.Infrastructure;
using Xunit;

namespace Sukurini.Tests.Infrastructure;

public class ThemeManagerTests
{
    [Fact]
    public void AppSettings_ChangesThemePropertyCorrectly()
    {
        var settings = AppSettings.Shared;
        bool eventFired = false;
        settings.ThemeChanged += () => eventFired = true;

        settings.Theme = AppTheme.Light;
        Assert.Equal(AppTheme.Light, settings.Theme);
        Assert.True(eventFired);

        settings.Theme = AppTheme.Dark;
        Assert.Equal(AppTheme.Dark, settings.Theme);
    }
}
