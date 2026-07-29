using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sukurini.Core;
using Sukurini.Infrastructure;

namespace Sukurini.Preferences;

public partial class PreferencesViewModel : ObservableObject
{
    private static readonly (uint Modifiers, uint KeyCode)[] HotKeyPresets = new[]
    {
        ((uint)5, (uint)83),  // Alt + Shift + S (Default)
        ((uint)6, (uint)83),  // Ctrl + Shift + S
        ((uint)12, (uint)83), // Win + Shift + S
        ((uint)1, (uint)83),  // Alt + S
        ((uint)3, (uint)83),  // Ctrl + Alt + S
        ((uint)5, (uint)71),  // Alt + Shift + G
    };

    [ObservableProperty]
    private bool _launchAtStartup;

    [ObservableProperty]
    private bool _ocrEnabled;

    [ObservableProperty]
    private bool _webpConversionEnabled;

    [ObservableProperty]
    private bool _semanticSearchEnabled;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private int _selectedDisposalIndex;

    [ObservableProperty]
    private int _selectedHotKeyIndex;

    public ObservableCollection<string> MonitoredFolders { get; } = new();

    public PreferencesViewModel()
    {
        _launchAtStartup = StartupManager.IsStartupEnabled();
        _ocrEnabled = AppSettings.Shared.OcrEnabled;
        _webpConversionEnabled = AppSettings.Shared.WebpConversionEnabled;
        _semanticSearchEnabled = AppSettings.Shared.SemanticSearchEnabled;
        _selectedThemeIndex = (int)AppSettings.Shared.Theme;
        _selectedDisposalIndex = (int)AppSettings.Shared.WebpDisposal;

        var currentBinding = AppSettings.Shared.GalleryHotKey;
        int idx = Array.FindIndex(HotKeyPresets, p => p.Modifiers == currentBinding.Modifiers && p.KeyCode == currentBinding.KeyCode);
        _selectedHotKeyIndex = idx >= 0 ? idx : 0;

        foreach (var folder in AppSettings.Shared.Folders)
        {
            MonitoredFolders.Add(folder);
        }
    }

    partial void OnSelectedHotKeyIndexChanged(int value)
    {
        if (value >= 0 && value < HotKeyPresets.Length)
        {
            var preset = HotKeyPresets[value];
            AppSettings.Shared.GalleryHotKey = new HotKeyBinding(preset.KeyCode, preset.Modifiers);
        }
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(AppTheme), value))
        {
            AppSettings.Shared.Theme = (AppTheme)value;
        }
    }

    partial void OnSelectedDisposalIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(WebPDisposal), value))
        {
            AppSettings.Shared.WebpDisposal = (WebPDisposal)value;
        }
    }

    partial void OnLaunchAtStartupChanged(bool value)
    {
        StartupManager.SetStartupEnabled(value);
    }

    partial void OnOcrEnabledChanged(bool value)
    {
        AppSettings.Shared.OcrEnabled = value;
    }

    partial void OnWebpConversionEnabledChanged(bool value)
    {
        AppSettings.Shared.WebpConversionEnabled = value;
    }

    partial void OnSemanticSearchEnabledChanged(bool value)
    {
        AppSettings.Shared.SemanticSearchEnabled = value;
    }

    [RelayCommand]
    private void ResetGallerySize()
    {
        AppSettings.Shared.ResetGallerySizeToDefault();
    }
}
