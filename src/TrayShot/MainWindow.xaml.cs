using System;
using System.Windows;
using System.Windows.Interop;
using TrayShot.About;
using TrayShot.Convert;
using TrayShot.Core;
using TrayShot.Gallery;
using TrayShot.Infrastructure;
using TrayShot.Preferences;
using TrayShot.StatusBar;

namespace TrayShot;

public partial class MainWindow : Window
{
    private TrayIconController? _trayController;
    private GalleryWindow? _galleryWindow;
    private PreferencesWindow? _preferencesWindow;
    private AboutWindow? _aboutWindow;
    private HotKeyManager? _hotKeyManager;

    public MainWindow()
    {
        InitializeComponent();
        InitializeApp();
    }

    private void InitializeApp()
    {
        Log.Initialize();
        Log.App.Info("Initializing TrayShot Windows application...");
        ThemeManager.Initialize();

        // Initialize Store
        string activeFolder = ScreencaptureDefaults.GetCurrentLocation();
        ScreenshotStore.Shared.Initialize(activeFolder, AppSettings.Shared.IncludeSubfolders);

        // Initialize Conversion Coordinator
        ConversionCoordinator.Shared.Initialize();

        // Initialize Gallery Window
        _galleryWindow = new GalleryWindow();
        _galleryWindow.OpenSettingsRequested += OpenPreferences;
        _galleryWindow.OpenAboutRequested += OpenAbout;
        _galleryWindow.ExitAppRequested += ExitApplication;

        // Initialize Tray Controller
        _trayController = new TrayIconController(
            onToggleGallery: () => _galleryWindow.Toggle(),
            onOpenPreferences: OpenPreferences,
            onOpenAbout: OpenAbout,
            onExitApp: ExitApplication);
        _trayController.Initialize();

        // Initialize HotKeyManager (Force Handle Creation since window is Hidden)
        var helper = new WindowInteropHelper(this);
        helper.EnsureHandle();
        var handle = helper.Handle;

        _hotKeyManager = new HotKeyManager(handle, () =>
        {
            Dispatcher.Invoke(() =>
            {
                Log.App.Info("Hotkey pressed, toggling gallery window...");
                _galleryWindow?.Toggle();
            });
        });

        RegisterCurrentHotKey();
        AppSettings.Shared.HotKeyChanged += OnHotKeySettingChanged;
    }

    private void RegisterCurrentHotKey()
    {
        var binding = AppSettings.Shared.GalleryHotKey;
        _hotKeyManager?.Register(binding.Modifiers, binding.KeyCode);
    }

    private void OnHotKeySettingChanged()
    {
        Dispatcher.Invoke(RegisterCurrentHotKey);
    }

    private void OpenPreferences()
    {
        if (_galleryWindow != null && !_galleryWindow.IsVisible)
        {
            _galleryWindow.Show();
        }

        if (_preferencesWindow == null || !_preferencesWindow.IsLoaded)
        {
            _preferencesWindow = new PreferencesWindow();
            if (_galleryWindow != null)
            {
                _preferencesWindow.Owner = _galleryWindow;
                _preferencesWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            _preferencesWindow.Show();
        }
        else
        {
            _preferencesWindow.Activate();
        }
    }

    private void OpenAbout()
    {
        if (_galleryWindow != null && !_galleryWindow.IsVisible)
        {
            _galleryWindow.Show();
        }

        if (_aboutWindow == null || !_aboutWindow.IsLoaded)
        {
            _aboutWindow = new AboutWindow();
            if (_galleryWindow != null)
            {
                _aboutWindow.Owner = _galleryWindow;
                _aboutWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            _aboutWindow.Show();
        }
        else
        {
            _aboutWindow.Activate();
        }
    }

    private void ExitApplication()
    {
        _hotKeyManager?.Dispose();
        _trayController?.Dispose();
        _galleryWindow?.Close();
        _preferencesWindow?.Close();
        _aboutWindow?.Close();
        Application.Current.Shutdown();
    }
}