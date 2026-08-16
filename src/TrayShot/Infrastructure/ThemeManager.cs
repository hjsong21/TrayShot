using System;
using System.Windows;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace TrayShot.Infrastructure;

public static class ThemeManager
{
    public static void Initialize()
    {
        AppSettings.Shared.ThemeChanged += () => ApplyTheme(AppSettings.Shared.Theme);
        ApplyTheme(AppSettings.Shared.Theme);
    }

    public static void ApplyTheme(AppTheme theme)
    {
        try
        {
            switch (theme)
            {
                case AppTheme.Light:
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    SetCustomThemeResources(isDark: false);
                    break;
                case AppTheme.System:
                    ApplicationThemeManager.ApplySystemTheme();
                    var sysTheme = ApplicationThemeManager.GetSystemTheme();
                    bool systemIsDark = sysTheme == SystemTheme.Dark || sysTheme != SystemTheme.Light;
                    SetCustomThemeResources(systemIsDark);
                    break;
                case AppTheme.Dark:
                default:
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    SetCustomThemeResources(isDark: true);
                    break;
            }

            Log.App.Info($"Applied theme: {theme}");
        }
        catch (Exception ex)
        {
            Log.App.Error($"Failed to apply theme {theme}: {ex.Message}");
        }
    }

    private static void SetCustomThemeResources(bool isDark)
    {
        var appResources = Application.Current?.Resources;
        if (appResources == null) return;

        if (isDark)
        {
            appResources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            appResources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A));
            appResources["BorderBrushColor"] = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
            appResources["TextPrimaryBrush"] = new SolidColorBrush(Colors.White);
            appResources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            appResources["ItemCardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));

            // TabControl Brushes (Dark Theme - Unselected matches Light Theme unselected)
            appResources["TabSelectedBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)); // Accent Blue (#2563EB)
            appResources["TabSelectedTextBrush"] = new SolidColorBrush(Colors.White);
            appResources["TabSelectedBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
            appResources["TabUnselectedBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)); // Light gray (Same as Light Theme)
            appResources["TabUnselectedTextBrush"] = new SolidColorBrush(Color.FromRgb(0x4B, 0x55, 0x63)); // Darker gray text (Same as Light Theme)
            appResources["TabUnselectedBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB)); // Same as Light Theme
            appResources["TabHoverBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB)); // Same as Light Theme
        }
        else
        {
            appResources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
            appResources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            appResources["BorderBrushColor"] = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
            appResources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            appResources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            appResources["ItemCardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA));

            // TabControl Brushes (Light Theme)
            appResources["TabSelectedBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB)); // Accent Blue (#2563EB)
            appResources["TabSelectedTextBrush"] = new SolidColorBrush(Colors.White);
            appResources["TabSelectedBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8));
            appResources["TabUnselectedBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)); // Light gray
            appResources["TabUnselectedTextBrush"] = new SolidColorBrush(Color.FromRgb(0x4B, 0x55, 0x63)); // Darker gray text
            appResources["TabUnselectedBorderBrush"] = new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB));
            appResources["TabHoverBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB));
        }
    }
}
