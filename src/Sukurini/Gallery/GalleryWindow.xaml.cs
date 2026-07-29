using System;
using System.Windows;
using System.Windows.Input;
using Sukurini.Infrastructure;
using Sukurini.Models;

namespace Sukurini.Gallery;

public partial class GalleryWindow : Window
{
    private readonly GalleryViewModel _viewModel;

    public GalleryWindow()
    {
        InitializeComponent();
        _viewModel = new GalleryViewModel();
        DataContext = _viewModel;

        _viewModel.OpenPreviewRequested += OnOpenPreview;
    }

    public void Toggle()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            PositionNearTray();
            Show();
            Activate();
            _viewModel.RefreshList();
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
