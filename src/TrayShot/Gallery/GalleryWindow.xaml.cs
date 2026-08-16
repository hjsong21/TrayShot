using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using TrayShot.Core;
using TrayShot.Infrastructure;
using TrayShot.Models;
using Point = System.Windows.Point;

namespace TrayShot.Gallery;

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

    private int? _preferredColumn;
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
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
                {
                    UpdateAllItemSelectionVisuals();
                });
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
            _preferredColumn = null;
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
            _preferredColumn = null;
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
            NavigateVertical(-1);
            e.Handled = true;
        }
        else if (e.Key == Key.Down && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            NavigateVertical(1);
            e.Handled = true;
        }
        else if (e.Key == Key.Home && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            _preferredColumn = null;
            if (_viewModel.FilteredScreenshots.Count > 0)
            {
                _viewModel.SelectedItem = _viewModel.FilteredScreenshots[0];
                EnsureSelectedItemVisible();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.End && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            _preferredColumn = null;
            if (_viewModel.FilteredScreenshots.Count > 0)
            {
                _viewModel.SelectedItem = _viewModel.FilteredScreenshots[_viewModel.FilteredScreenshots.Count - 1];
                EnsureSelectedItemVisible();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.PageUp && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            NavigateVertical(-GetRowsPerPage());
            e.Handled = true;
        }
        else if (e.Key == Key.PageDown && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            NavigateVertical(GetRowsPerPage());
            e.Handled = true;
        }
    }

    private void OnListBoxItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Legacy - kept for compatibility but not called from new grouped layout
    }

    private Point _dragStartPoint;

    private void OnItemPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        _preferredColumn = null;
        if (sender is ScreenshotItemControl itemControl && itemControl.ScreenshotItem != null)
        {
            _viewModel.SelectedItem = itemControl.ScreenshotItem;
            itemControl.Focus();
        }
    }

    private void OnItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _preferredColumn = null;
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

    private int GetRowsPerPage()
    {
        double height = GalleryScrollViewer.ActualHeight;
        if (height <= 0) height = Height - 60;
        int rows = Math.Max(1, (int)(height / 136));
        return rows;
    }

    private record GridRowInfo(int GroupIndex, int RowIndexInGroup, List<Screenshot> Items);

    private List<GridRowInfo> BuildGridRows(int columnsCount)
    {
        var rows = new List<GridRowInfo>();
        for (int g = 0; g < _viewModel.GroupedScreenshots.Count; g++)
        {
            var groupItems = _viewModel.GroupedScreenshots[g].Items;
            int rowCountInGroup = (int)Math.Ceiling((double)groupItems.Count / columnsCount);
            for (int r = 0; r < rowCountInGroup; r++)
            {
                var rowItems = groupItems.Skip(r * columnsCount).Take(columnsCount).ToList();
                rows.Add(new GridRowInfo(g, r, rowItems));
            }
        }
        return rows;
    }

    private void NavigateVertical(int rowOffset)
    {
        if (_viewModel.GroupedScreenshots.Count == 0) return;

        if (_viewModel.SelectedItem == null)
        {
            if (_viewModel.FilteredScreenshots.Count > 0)
            {
                _viewModel.SelectedItem = _viewModel.FilteredScreenshots[0];
                EnsureSelectedItemVisible();
            }
            return;
        }

        int columnsCount = GetColumnsCount();
        var allRows = BuildGridRows(columnsCount);
        if (allRows.Count == 0) return;

        int currentRowIndex = -1;
        int currentColIndex = -1;

        for (int r = 0; r < allRows.Count; r++)
        {
            int col = allRows[r].Items.IndexOf(_viewModel.SelectedItem);
            if (col >= 0)
            {
                currentRowIndex = r;
                currentColIndex = col;
                break;
            }
        }

        if (currentRowIndex < 0)
        {
            currentRowIndex = 0;
            currentColIndex = 0;
        }

        if (!_preferredColumn.HasValue)
        {
            _preferredColumn = currentColIndex;
        }

        int targetRowIndex = Math.Clamp(currentRowIndex + rowOffset, 0, allRows.Count - 1);
        var targetRow = allRows[targetRowIndex];

        int targetColIndex = Math.Min(_preferredColumn.Value, targetRow.Items.Count - 1);
        _viewModel.SelectedItem = targetRow.Items[targetColIndex];
        EnsureSelectedItemVisible();
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
            Point currentPos = e.GetPosition(this);
            Vector diff = _dragStartPoint - currentPos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                string path = _viewModel.SelectedItem.Path;
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

                try
                {
                    var dataObj = CreateDragDataObject(path);
                    DragDrop.DoDragDrop(this, dataObj, DragDropEffects.Copy);
                }
                catch (Exception ex)
                {
                    Log.App.Error($"Drag drop failed: {ex.Message}");
                }
            }
        }
    }

    private static DataObject CreateDragDataObject(string imagePath)
    {
        var dataObj = new DataObject();
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) return dataObj;

        string ext = Path.GetExtension(imagePath).ToLowerInvariant();

        // 1. Generate a single temporary PNG file for non-PNG images (e.g. .webp)
        // so that all web/Electron apps (Antigravity 2.0) and chat apps (KakaoTalk) receive a single valid PNG file.
        string targetFilePath = imagePath;
        if (ext != ".png")
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "TrayShotTemp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string tempPngName = $"{Path.GetFileNameWithoutExtension(imagePath)}_drag.png";
                string tempPngPath = Path.Combine(tempDir, tempPngName);

                using (var img = ThumbnailLoader.LoadImageUniversal(imagePath))
                {
                    img.SaveAsPng(tempPngPath);
                }
                targetFilePath = tempPngPath;
            }
            catch (Exception ex)
            {
                Log.App.Warn($"Failed to create temp PNG for drag-drop: {ex.Message}");
            }
        }

        // 2. Set FileDrop list with EXACTLY ONE single file path to prevent duplicate item insertion
        var fileList = new System.Collections.Specialized.StringCollection { targetFilePath };
        dataObj.SetFileDropList(fileList);
        dataObj.SetData(DataFormats.FileDrop, new string[] { targetFilePath });

        // 3. Set DeviceIndependentBitmap (CF_DIB) stream for MS Office (Word/PPT), HWP (한글), and Paint (그림판)
        try
        {
            using var img = ThumbnailLoader.LoadImageUniversal(targetFilePath);
            using var bmpMs = new MemoryStream();
            img.SaveAsBmp(bmpMs);
            byte[] bmpBytes = bmpMs.ToArray();

            // BMP file header is 14 bytes (0x00..0x0D).
            // CF_DIB is the BMP payload starting immediately after the 14-byte BITMAPFILEHEADER.
            if (bmpBytes.Length > 14)
            {
                byte[] dibBytes = new byte[bmpBytes.Length - 14];
                Array.Copy(bmpBytes, 14, dibBytes, 0, dibBytes.Length);
                var dibStream = new MemoryStream(dibBytes);
                dataObj.SetData("DeviceIndependentBitmap", dibStream);
            }
        }
        catch (Exception dibEx)
        {
            Log.App.Warn($"Failed to set DeviceIndependentBitmap for drag-drop: {dibEx.Message}");
        }

        // 4. Set WPF BitmapSource & SetImage for WPF/Standard apps
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(targetFilePath);
            bitmap.EndInit();
            bitmap.Freeze();

            dataObj.SetImage(bitmap);
            dataObj.SetData(DataFormats.Bitmap, bitmap);
        }
        catch (Exception bmpEx)
        {
            Log.App.Warn($"Failed to set Bitmap for drag-drop: {bmpEx.Message}");
        }

        return dataObj;
    }

    public event Action? OpenSettingsRequested;
    public event Action? OpenAboutRequested;
    public event Action? ExitAppRequested;

    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu != null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
        }
    }

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
            previewWin.CurrentItemChanged += (selectedItem) =>
            {
                _viewModel.SelectedItem = selectedItem;
                EnsureSelectedItemVisible();
            };
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
