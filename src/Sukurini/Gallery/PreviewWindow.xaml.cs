using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Sukurini.Models;

namespace Sukurini.Gallery;

public partial class PreviewWindow : Window
{
    private readonly Screenshot _screenshot;

    public PreviewWindow(Screenshot screenshot)
    {
        InitializeComponent();
        _screenshot = screenshot;

        TitleText.Text = screenshot.Name;
        LoadImage(screenshot.Path);
    }

    private void LoadImage(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                PreviewImage.Source = bitmap;
            }
        }
        catch
        {
            // Error loading image
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape || e.Key == Key.Space)
        {
            Close();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
