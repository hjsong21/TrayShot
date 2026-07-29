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
        _selectedDisposalIndex = (int)AppSettings.Shared.WebpDisposal;

        var currentBinding = AppSettings.Shared.GalleryHotKey;
        HotKeyDisplayText = HotKeyFormatter.Format(currentBinding.Modifiers, currentBinding.KeyCode);
        HotKeyStatusText = "✓ 현재 사용 중인 전역 단축키";
        HotKeyStatusBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // Green

        foreach (var folder in AppSettings.Shared.Folders)
        {
            MonitoredFolders.Add(folder);
        }
    }

    public void TrySetHotKey(uint modifiers, uint keyCode)
    {
        // 1. Check if hotkey is valid (must have a modifier or be an F-key)
        bool isFKey = keyCode >= (uint)KeyInterop.VirtualKeyFromKey(Key.F1) && keyCode <= (uint)KeyInterop.VirtualKeyFromKey(Key.F24);
        if (modifiers == 0 && !isFKey)
        {
            HasConflict = true;
            HotKeyDisplayText = HotKeyFormatter.Format(modifiers, keyCode);
            HotKeyStatusText = "⚠️ 단축키는 조합키(Ctrl, Alt, Shift 등) 또는 기능키(F1~F12)를 포함해야 합니다. 다시 입력해 주세요.";
            HotKeyStatusBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52)); // Red
            HotKeyBorderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x52, 0x52));
            return;
        }

        // 2. Test availability (conflict check)
        bool available = HotKeyManager.TestAvailability(modifiers, keyCode);
        HotKeyDisplayText = HotKeyFormatter.Format(modifiers, keyCode);

        if (available)
        {
            HasConflict = false;
            AppSettings.Shared.GalleryHotKey = new HotKeyBinding(keyCode, modifiers);
            HotKeyStatusText = "✓ 전역 단축키가 성공적으로 변경되었습니다.";
            HotKeyStatusBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // Green
            HotKeyBorderBrush = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A));
        }
        else
        {
            HasConflict = true;
            HotKeyStatusText = "❌ 충돌: 다른 앱 또는 시스템에서 이미 사용 중입니다. 다시 입력해 주세요.";
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
