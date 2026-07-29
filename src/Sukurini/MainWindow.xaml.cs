using System;
using System.Windows;
using System.Windows.Interop;
using Sukurini.Convert;
using Sukurini.Core;
using Sukurini.Gallery;
using Sukurini.Infrastructure;
using Sukurini.Preferences;
using Sukurini.StatusBar;

namespace Sukurini;

public partial class MainWindow : Window
{
    private TrayIconController? _trayController;
    private GalleryWindow? _galleryWindow;
    private PreferencesWindow? _preferencesWindow;
    private HotKeyManager? _hotKeyManager;

    public MainWindow()
    {
        InitializeComponent();
        InitializeApp();
    }

    private void InitializeApp()
    {
        Log.Initialize();
        Log.App.Info("Initializing Sukurini Windows application...");
        ThemeManager.Initialize();

        // Initialize Store
        string activeFolder = ScreencaptureDefaults.GetCurrentLocation();
        ScreenshotStore.Shared.Initialize(activeFolder, AppSettings.Shared.IncludeSubfolders);

        // Initialize Conversion Coordinator
        ConversionCoordinator.Shared.Initialize();

        // Initialize Gallery Window
        _galleryWindow = new GalleryWindow();

        // Initialize Tray Controller
        _trayController = new TrayIconController(
            onToggleGallery: () => _galleryWindow.Toggle(),
            onOpenPreferences: OpenPreferences,
            onExitApp: ExitApplication);
        _trayController.Initialize();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            _hotKeyManager = new HotKeyManager(handle, () =>
            {
                Dispatcher.Invoke(() => _galleryWindow?.Toggle());
            });

            RegisterCurrentHotKey();
            AppSettings.Shared.HotKeyChanged += OnHotKeySettingChanged;
        }
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
        if (_preferencesWindow == null || !_preferencesWindow.IsLoaded)
        {
            _preferencesWindow = new PreferencesWindow();
            _preferencesWindow.Show();
        }
        else
        {
            _preferencesWindow.Activate();
        }
    }

    private void ExitApplication()
    {
        _hotKeyManager?.Dispose();
        _trayController?.Dispose();
        _galleryWindow?.Close();
        _preferencesWindow?.Close();
        Application.Current.Shutdown();
    }
}