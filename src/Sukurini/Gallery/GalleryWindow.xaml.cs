using System;
using System.Collections.Generic;
using System.Linq;
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

        _viewModel.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(GalleryViewModel.SelectedItem))
            {
                UpdateAllItemSelectionVisuals();
            }
        };

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

    private bool _isChildWindowOpen;

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (_isChildWindowOpen)
        {
            return;
        }

        if ((DateTime.UtcNow - _lastToggleTime).TotalMilliseconds < 350)
        {
            return;
        }

        Hide();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (_viewModel.HasSearchQuery)
            {
                _viewModel.SearchQuery = string.Empty;
                e.Handled = true;
            }
            else
            {
                Hide();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Delete && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            if (_viewModel.SelectedItem != null)
            {
                _viewModel.DeleteSelectedCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            var restored = GalleryViewModel.UndoDelete();
            if (restored != null)
            {
                _viewModel.SelectedItem = System.Linq.Enumerable.FirstOrDefault(_viewModel.FilteredScreenshots, i => i.Path.Equals(restored.Path, StringComparison.OrdinalIgnoreCase));
            }
            e.Handled = true;
        }
        else if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            if (_viewModel.SelectedItem != null)
            {
                _viewModel.CopySelectedCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            _viewModel.PasteClipboardCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Space && _viewModel.SelectedItem != null && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            OnOpenPreview(_viewModel.SelectedItem);
            e.Handled = true;
        }
        else if (e.Key == Key.Left && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            if (_viewModel.FilteredScreenshots.Count > 0)
            {
                int currentIndex = _viewModel.SelectedItem != null ? _viewModel.FilteredScreenshots.IndexOf(_viewModel.SelectedItem) : -1;
                int newIndex = currentIndex > 0 ? currentIndex - 1 : 0;
                _viewModel.SelectedItem = _viewModel.FilteredScreenshots[newIndex];
                EnsureSelectedItemVisible();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Right && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            if (_viewModel.FilteredScreenshots.Count > 0)
            {
                int currentIndex = _viewModel.SelectedItem != null ? _viewModel.FilteredScreenshots.IndexOf(_viewModel.SelectedItem) : -1;
                int newIndex = currentIndex < _viewModel.FilteredScreenshots.Count - 1 ? currentIndex + 1 : _viewModel.FilteredScreenshots.Count - 1;
                _viewModel.SelectedItem = _viewModel.FilteredScreenshots[newIndex];
                EnsureSelectedItemVisible();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Up && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            if (_viewModel.FilteredScreenshots.Count > 0)
            {
                int columnsCount = GetColumnsCount();
                int currentIndex = _viewModel.SelectedItem != null ? _viewModel.FilteredScreenshots.IndexOf(_viewModel.SelectedItem) : 0;
                int newIndex = Math.Max(0, currentIndex - columnsCount);
                _viewModel.SelectedItem = _viewModel.FilteredScreenshots[newIndex];
                EnsureSelectedItemVisible();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Down && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            if (_viewModel.FilteredScreenshots.Count > 0)
            {
                int columnsCount = GetColumnsCount();
                int currentIndex = _viewModel.SelectedItem != null ? _viewModel.FilteredScreenshots.IndexOf(_viewModel.SelectedItem) : -1;
                int newIndex = Math.Min(_viewModel.FilteredScreenshots.Count - 1, currentIndex + columnsCount);
                _viewModel.SelectedItem = _viewModel.FilteredScreenshots[newIndex];
                EnsureSelectedItemVisible();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Home && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            if (_viewModel.FilteredScreenshots.Count > 0)
            {
                _viewModel.SelectedItem = _viewModel.FilteredScreenshots[0];
                EnsureSelectedItemVisible();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.End && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            if (_viewModel.FilteredScreenshots.Count > 0)
            {
                _viewModel.SelectedItem = _viewModel.FilteredScreenshots[_viewModel.FilteredScreenshots.Count - 1];
                EnsureSelectedItemVisible();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.PageUp && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            if (_viewModel.FilteredScreenshots.Count > 0)
            {
                int pageSize = GetPageSize();
                int currentIndex = _viewModel.SelectedItem != null ? _viewModel.FilteredScreenshots.IndexOf(_viewModel.SelectedItem) : 0;
                int newIndex = Math.Max(0, currentIndex - pageSize);
                _viewModel.SelectedItem = _viewModel.FilteredScreenshots[newIndex];
                EnsureSelectedItemVisible();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.PageDown && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            if (_viewModel.FilteredScreenshots.Count > 0)
            {
                int pageSize = GetPageSize();
                int currentIndex = _viewModel.SelectedItem != null ? _viewModel.FilteredScreenshots.IndexOf(_viewModel.SelectedItem) : -1;
                int newIndex = Math.Min(_viewModel.FilteredScreenshots.Count - 1, currentIndex + pageSize);
                _viewModel.SelectedItem = _viewModel.FilteredScreenshots[newIndex];
                EnsureSelectedItemVisible();
                e.Handled = true;
            }
        }
    }

    private void OnListBoxItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Legacy - kept for compatibility but not called from new grouped layout
    }

    private void OnItemPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ScreenshotItemControl itemControl && itemControl.ScreenshotItem != null)
        {
            _viewModel.SelectedItem = itemControl.ScreenshotItem;
            itemControl.Focus();
            e.Handled = true;
        }
    }

    private void OnItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ScreenshotItemControl itemControl && itemControl.ScreenshotItem != null)
        {
            _viewModel.SelectedItem = itemControl.ScreenshotItem;
            itemControl.Focus();
        }
    }

    private void OnItemMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedItem != null)
        {
            OnOpenPreview(_viewModel.SelectedItem);
        }
    }

    /// <summary>
    /// Refreshes the IsSelected visual state on all ScreenshotItemControl instances.
    /// Called whenever SelectedItem changes.
    /// </summary>
    private void UpdateAllItemSelectionVisuals()
    {
        foreach (var group in _viewModel.GroupedScreenshots)
        {
            foreach (var item in group.Items)
            {
                var container = FindItemControl(item);
                if (container != null)
                {
                    container.IsSelected = ReferenceEquals(item, _viewModel.SelectedItem);
                }
            }
        }
    }

    private int GetColumnsCount()
    {
        double width = GalleryScrollViewer.ActualWidth;
        if (width <= 0) width = Width - 40;
        int cols = (int)(width / 192);
        return Math.Max(1, cols);
    }

    private int GetPageSize()
    {
        double height = GalleryScrollViewer.ActualHeight;
        if (height <= 0) height = Height - 60;
        int rows = Math.Max(1, (int)(height / 136));
        return GetColumnsCount() * rows;
    }

    private void EnsureSelectedItemVisible()
    {
        if (_viewModel.SelectedItem == null) return;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, () =>
        {
            var container = FindItemControl(_viewModel.SelectedItem);
            container?.BringIntoView();
        });
    }

    /// <summary>
    /// Walks the visual tree to find the ScreenshotItemControl for a given Screenshot.
    /// </summary>
    private ScreenshotItemControl? FindItemControl(Screenshot target)
    {
        return FindVisualChildren<ScreenshotItemControl>(this)
            .FirstOrDefault(c => ReferenceEquals(c.ScreenshotItem, target));
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                yield return match;
            foreach (var grandchild in FindVisualChildren<T>(child))
                yield return grandchild;
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

    public event Action? OpenSettingsRequested;
    public event Action? OpenAboutRequested;
    public event Action? ExitAppRequested;

    private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
    {
        _isChildWindowOpen = true;
        try
        {
            OpenSettingsRequested?.Invoke();
        }
        finally
        {
            _isChildWindowOpen = false;
        }
    }

    private void OnOpenAboutClick(object sender, RoutedEventArgs e)
    {
        _isChildWindowOpen = true;
        try
        {
            OpenAboutRequested?.Invoke();
        }
        finally
        {
            _isChildWindowOpen = false;
        }
    }

    private void OnExitAppClick(object sender, RoutedEventArgs e)
    {
        ExitAppRequested?.Invoke();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void OnOpenPreview(Screenshot item)
    {
        _isChildWindowOpen = true;
        try
        {
            var previewWin = new PreviewWindow(item, System.Linq.Enumerable.ToList(_viewModel.FilteredScreenshots));
            previewWin.Owner = this;
            previewWin.ShowDialog();
        }
        finally
        {
            _isChildWindowOpen = false;
            Activate();
            Focus();
        }
    }
}
