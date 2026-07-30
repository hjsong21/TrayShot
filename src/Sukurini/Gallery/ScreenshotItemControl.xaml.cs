using System.Windows;
using System.Windows.Controls;
using Sukurini.Core;
using Sukurini.Models;

namespace Sukurini.Gallery;

public partial class ScreenshotItemControl : UserControl
{
    public static readonly DependencyProperty ScreenshotItemProperty =
        DependencyProperty.Register(
            nameof(ScreenshotItem),
            typeof(Screenshot),
            typeof(ScreenshotItemControl),
            new PropertyMetadata(null, OnScreenshotItemChanged));

    public Screenshot? ScreenshotItem
    {
        get => (Screenshot?)GetValue(ScreenshotItemProperty);
        set => SetValue(ScreenshotItemProperty, value);
    }

    public ScreenshotItemControl()
    {
        InitializeComponent();
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
}
