using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Sukurini.Infrastructure;
using Sukurini.Models;

namespace Sukurini.Gallery;

public partial class GalleryWindow : Window
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTLEFT = 10;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;

    private readonly GalleryViewModel _viewModel;

    public GalleryWindow()
    {
        InitializeComponent();
        _viewModel = new GalleryViewModel();
        DataContext = _viewModel;

        _viewModel.OpenPreviewRequested += OnOpenPreview;

        Width = AppSettings.Shared.GalleryWidth;
        Height = AppSettings.Shared.GalleryHeight;

        SizeChanged += OnGallerySizeChanged;
        AppSettings.Shared.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void OnGallerySizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsLoaded && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            AppSettings.Shared.GalleryWidth = e.NewSize.Width;
            AppSettings.Shared.GalleryHeight = e.NewSize.Height;
            PositionNearTray();
        }
    }

    private void OnSettingsPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppSettings.GalleryWidth) || e.PropertyName == nameof(AppSettings.GalleryHeight))
        {
            Dispatcher.Invoke(() =>
            {
                Width = AppSettings.Shared.GalleryWidth;
                Height = AppSettings.Shared.GalleryHeight;
                PositionNearTray();
            });
        }
    }

    private void ResizeLeft_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            PerformNativeResize(HTLEFT);
        }
    }

    private void ResizeTop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            PerformNativeResize(HTTOP);
        }
    }

    private void ResizeTopLeft_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            PerformNativeResize(HTTOPLEFT);
        }
    }

    private void PerformNativeResize(int hitTestCode)
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                ReleaseCapture();
                SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)hitTestCode, IntPtr.Zero);
            }
        }
        catch (Exception ex)
        {
            Log.App.Error($"Native resize failed: {ex.Message}");
        }
    }

    private DateTime _lastToggleTime = DateTime.MinValue;

    public void Toggle()
    {
        _lastToggleTime = DateTime.UtcNow;

        if (IsVisible)
        {
            Hide();
        }
        else
        {
            PositionNearTray();
            Show();
            Topmost = true;
            Activate();
            Focus();
            _viewModel.RefreshList();
            Log.App.Info("GalleryWindow toggled to VISIBLE");
        }
    }

    private void PositionNearTray()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if ((DateTime.UtcNow - _lastToggleTime).TotalMilliseconds < 350)
        {
            return;
        }

        Hide();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
        }
        else if (e.Key == Key.Space && _viewModel.SelectedItem != null)
        {
            OnOpenPreview(_viewModel.SelectedItem);
        }
    }

    private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedItem != null)
        {
            OnOpenPreview(_viewModel.SelectedItem);
        }
    }

    private void OnItemPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _viewModel.SelectedItem != null)
        {
            try
            {
                var dataObj = new DataObject(DataFormats.FileDrop, new string[] { _viewModel.SelectedItem.Path });
                DragDrop.DoDragDrop(this, dataObj, DragDropEffects.Copy);
            }
            catch (Exception ex)
            {
                Log.App.Error($"Drag drop failed: {ex.Message}");
            }
        }
    }

    private void OnOpenPreview(Screenshot item)
    {
        var previewWin = new PreviewWindow(item);
        previewWin.Owner = this;
        previewWin.ShowDialog();
    }
}
