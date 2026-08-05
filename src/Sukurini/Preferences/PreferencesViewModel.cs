using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sukurini.Core;
using Sukurini.Infrastructure;

namespace Sukurini.Preferences;

public partial class PreferencesViewModel : ObservableObject
{
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
    private int _selectedLanguageIndex;

    [ObservableProperty]
    private int _selectedDisposalIndex;

    [ObservableProperty]
    private string _hotKeyDisplayText = "";

    [ObservableProperty]
    private string _hotKeyStatusText = "";

    [ObservableProperty]
    private bool _hasConflict;

    [ObservableProperty]
    private Brush _hotKeyStatusBrush = new SolidColorBrush(Color.FromRgb(136, 136, 136));

    [ObservableProperty]
    private Brush _hotKeyBorderBrush = new SolidColorBrush(Color.FromRgb(58, 58, 58));

    public ObservableCollection<string> MonitoredFolders { get; } = new();

    public PreferencesViewModel()
    {
        _launchAtStartup = StartupManager.IsStartupEnabled();
        _ocrEnabled = AppSettings.Shared.OcrEnabled;
        _webpConversionEnabled = AppSettings.Shared.WebpConversionEnabled;
        _semanticSearchEnabled = AppSettings.Shared.SemanticSearchEnabled;
        _selectedThemeIndex = (int)AppSettings.Shared.Theme;

        string currentLang = AppSettings.Shared.Language;
        _selectedLanguageIndex = currentLang.Equals("ko-KR", StringComparison.OrdinalIgnoreCase) || currentLang.Equals("ko", StringComparison.OrdinalIgnoreCase) ? 1 :
                                currentLang.Equals("en-US", StringComparison.OrdinalIgnoreCase) || currentLang.Equals("en", StringComparison.OrdinalIgnoreCase) ? 2 : 0;

        _selectedDisposalIndex = (int)AppSettings.Shared.WebpDisposal;

        var currentBinding = AppSettings.Shared.GalleryHotKey;
        HotKeyDisplayText = HotKeyFormatter.Format(currentBinding.Modifiers, currentBinding.KeyCode);
        HotKeyStatusText = LanguageManager.GetString("Pref_Hotkey_Status_Active");
        HotKeyStatusBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // Green

        foreach (var folder in AppSettings.Shared.Folders)
        {
            MonitoredFolders.Add(folder);
        }

        AppSettings.Shared.LanguageChanged += OnLanguageChanged;
    }

    public void TrySetHotKey(uint modifiers, uint keyCode)
    {
        var result = HotKeyValidator.Validate(modifiers, keyCode);
        HotKeyDisplayText = HotKeyFormatter.Format(modifiers, keyCode);

        if (result.IsValid)
        {
            HasConflict = false;
            AppSettings.Shared.GalleryHotKey = new HotKeyBinding(keyCode, modifiers);
            HotKeyStatusText = result.Message;
            HotKeyStatusBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // Green
            HotKeyBorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
        }
        else
        {
            HasConflict = true;
            HotKeyStatusText = result.Message;
            HotKeyStatusBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52)); // Red
            HotKeyBorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52));
        }
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(AppTheme), value))
        {
            AppSettings.Shared.Theme = (AppTheme)value;
        }
    }

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        string langCode = value switch
        {
            1 => "ko-KR",
            2 => "en-US",
            _ => "system"
        };
        AppSettings.Shared.Language = langCode;
    }

    private void OnLanguageChanged()
    {
        if (!HasConflict)
        {
            HotKeyStatusText = LanguageManager.GetString("Pref_Hotkey_Status_Active");
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
