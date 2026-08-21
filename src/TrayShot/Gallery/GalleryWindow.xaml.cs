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

        _viewModel.SelectionChanged += () =>
        {
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
            {
                UpdateAllItemSelectionVisuals();
            });
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

    public void ShowAndSelect(Screenshot? screenshot = null)
    {
        PositionNearTray();
        Show();
        Topmost = true;
        Activate();
        Focus();
        _viewModel.RefreshList();

        if (screenshot != null)
        {
            var match = System.Linq.Enumerable.FirstOrDefault(_viewModel.FilteredScreenshots, s => s.Path.Equals(screenshot.Path, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                _viewModel.SelectedItem = match;
                EnsureSelectedItemVisible();
            }
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
        else if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && !(e.OriginalSource is System.Windows.Controls.TextBox))
        {
            _viewModel.SelectAll();
            UpdateAllItemSelectionVisuals();
            e.Handled = true;
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
    private ScreenshotItemControl? _clickedItemControl;
    private bool _shouldReduceSelectionOnMouseUp;

    private void OnItemPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        _preferredColumn = null;
        _clickedItemControl = sender as ScreenshotItemControl;
        _shouldReduceSelectionOnMouseUp = false;

        if (sender is ScreenshotItemControl itemControl && itemControl.ScreenshotItem != null)
        {
            var item = itemControl.ScreenshotItem;
            bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

            if (isCtrl)
            {
                _viewModel.ToggleSelection(item);
                itemControl.Focus();
            }
            else
            {
                // If clicked item is already selected in a multi-selection group, don't clear multi-selection on MouseDown
                // so user can drag the selected group. Reduce to single selection on MouseUp if not dragged.
                if (_viewModel.IsSelected(item) && _viewModel.SelectedItems.Count > 1)
                {
                    _shouldReduceSelectionOnMouseUp = true;
                }
                else
                {
                    _viewModel.SetSingleSelection(item);
                }
                itemControl.Focus();
            }
            UpdateAllItemSelectionVisuals();
        }
    }

    private void OnItemPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_shouldReduceSelectionOnMouseUp && _clickedItemControl != null && _clickedItemControl.ScreenshotItem != null)
        {
            Point currentPos = e.GetPosition(this);
            Vector diff = _dragStartPoint - currentPos;

            if (Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance)
            {
                bool isCtrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                if (!isCtrl)
                {
                    _viewModel.SetSingleSelection(_clickedItemControl.ScreenshotItem);
                    UpdateAllItemSelectionVisuals();
                }
            }
        }
        _shouldReduceSelectionOnMouseUp = false;
        _clickedItemControl = null;
    }

    private void OnItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _preferredColumn = null;
        if (sender is ScreenshotItemControl itemControl && itemControl.ScreenshotItem != null)
        {
            var item = itemControl.ScreenshotItem;
            if (!_viewModel.IsSelected(item))
            {
                _viewModel.SetSingleSelection(item);
                UpdateAllItemSelectionVisuals();
            }
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
    /// Called whenever selection changes.
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
                    container.IsSelected = _viewModel.IsSelected(item);
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
        if (e.LeftButton == MouseButtonState.Pressed && (_viewModel.SelectedItems.Count > 0 || _viewModel.SelectedItem != null))
        {
            Point currentPos = e.GetPosition(this);
            Vector diff = _dragStartPoint - currentPos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _shouldReduceSelectionOnMouseUp = false;

                var targetItems = _viewModel.SelectedItems.Count > 0
                    ? _viewModel.SelectedItems.ToList()
                    : (_viewModel.SelectedItem != null ? new List<Screenshot> { _viewModel.SelectedItem } : new List<Screenshot>());

                var paths = targetItems.Select(s => s.Path).Where(p => !string.IsNullOrEmpty(p) && File.Exists(p)).ToList();
                if (paths.Count == 0) return;

                try
                {
                    var dataObj = DragDropHelper.CreateDragDataObject(paths);
                    DragDrop.DoDragDrop(this, dataObj, DragDropEffects.Copy);
                }
                catch (Exception ex)
                {
                    Log.App.Error($"Drag drop failed: {ex.Message}");
                }
            }
        }
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
