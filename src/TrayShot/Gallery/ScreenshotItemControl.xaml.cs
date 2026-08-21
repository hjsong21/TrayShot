using System.Windows;
using System.Windows.Controls;
using TrayShot.Core;
using TrayShot.Models;

namespace TrayShot.Gallery;

public partial class ScreenshotItemControl : UserControl
{
    public static readonly DependencyProperty ScreenshotItemProperty =
        DependencyProperty.Register(
            nameof(ScreenshotItem),
            typeof(Screenshot),
            typeof(ScreenshotItemControl),
            new PropertyMetadata(null, OnScreenshotItemChanged));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(ScreenshotItemControl),
            new PropertyMetadata(false, OnIsSelectedChanged));

    public Screenshot? ScreenshotItem
    {
        get => (Screenshot?)GetValue(ScreenshotItemProperty);
        set => SetValue(ScreenshotItemProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public ScreenshotItemControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (Tag is GalleryViewModel vm && ScreenshotItem != null)
        {
            IsSelected = vm.IsSelected(ScreenshotItem);
        }
        else
        {
            UpdateSelectionVisual();
        }
    }

    private static async void OnScreenshotItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScreenshotItemControl control && e.NewValue is Screenshot item)
        {
            control.FileNameText.Text = item.Name;
            control.VideoBadge.Visibility = ScreenshotFile.IsVideo(item.Path) ? Visibility.Visible : Visibility.Collapsed;

            string ext = System.IO.Path.GetExtension(item.Path).TrimStart('.').ToUpperInvariant();
            if (!string.IsNullOrEmpty(ext))
            {
                control.FormatBadgeText.Text = ext;
                control.FormatBadge.Visibility = Visibility.Visible;
            }
            else
            {
                control.FormatBadge.Visibility = Visibility.Collapsed;
            }

            var thumbnail = await ThumbnailLoader.Shared.LoadThumbnailAsync(item.Path, 360);
            if (thumbnail != null && control.ScreenshotItem == item)
            {
                control.ThumbnailImage.Source = thumbnail;
            }
        }
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScreenshotItemControl control)
        {
            control.UpdateSelectionVisual();
        }
    }

    private void UpdateSelectionVisual()
    {
        // The root Border is the first child of the control's visual tree
        if (Content is System.Windows.Controls.Border border)
        {
            border.BorderBrush = IsSelected
                ? new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x60, 0xA5, 0xFA)) // blue-400
                : (System.Windows.Media.Brush)FindResource("BorderBrushColor");
            border.BorderThickness = IsSelected
                ? new Thickness(2)
                : new Thickness(1);
        }
    }
}
