using System.Windows;
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

        // Initialize Gallery Window
        _galleryWindow = new GalleryWindow();

        // Initialize Tray Controller
        _trayController = new TrayIconController(
            onToggleGallery: () => _galleryWindow.Toggle(),
            onOpenPreferences: OpenPreferences,
            onExitApp: ExitApplication);
        _trayController.Initialize();
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
        _trayController?.Dispose();
        _galleryWindow?.Close();
        _preferencesWindow?.Close();
        Application.Current.Shutdown();
    }
}