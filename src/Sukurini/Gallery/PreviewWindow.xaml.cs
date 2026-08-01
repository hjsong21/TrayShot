using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Sukurini.Models;

namespace Sukurini.Gallery;

public partial class PreviewWindow : Window
{
    private readonly IList<Screenshot>? _items;
    private int _currentIndex;

    public event Action<Screenshot>? CurrentItemChanged;

    public PreviewWindow(Screenshot screenshot, IList<Screenshot>? items = null)
    {
        InitializeComponent();
        _items = items;

        if (_items != null && _items.Count > 0)
        {
            _currentIndex = _items.IndexOf(screenshot);
            if (_currentIndex < 0) _currentIndex = 0;
            ShowItemAt(_currentIndex);
        }
        else
        {
            TitleText.Text = screenshot.Name;
            LoadImage(screenshot.Path);
        }
    }

    private void ShowItemAt(int index)
    {
        if (_items == null || _items.Count == 0) return;

        if (index < 0) index = 0;
        if (index >= _items.Count) index = _items.Count - 1;

        _currentIndex = index;
        var current = _items[_currentIndex];

        TitleText.Text = _items.Count > 1 
            ? $"{current.Name} ({_currentIndex + 1} / {_items.Count})" 
            : current.Name;

        LoadImage(current.Path);
        CurrentItemChanged?.Invoke(current);
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
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            DeleteCurrentItem();
            e.Handled = true;
        }
        else if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            var restored = GalleryViewModel.UndoDelete();
            if (restored != null && _items != null)
            {
                _items.Add(restored);
                ShowItemAt(_items.Count - 1);
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Left || e.Key == Key.Up)
        {
            ShowItemAt(_currentIndex - 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Right || e.Key == Key.Down)
        {
            ShowItemAt(_currentIndex + 1);
            e.Handled = true;
        }
        else if (e.Key == Key.Home)
        {
            ShowItemAt(0);
            e.Handled = true;
        }
        else if (e.Key == Key.End)
        {
            ShowItemAt((_items?.Count ?? 1) - 1);
            e.Handled = true;
        }
    }

    private void DeleteCurrentItem()
    {
        if (_items == null || _items.Count == 0)
        {
            Close();
            return;
        }

        if (_currentIndex < 0 || _currentIndex >= _items.Count) return;

        var target = _items[_currentIndex];
        GalleryViewModel.DeleteScreenshot(target);
        _items.RemoveAt(_currentIndex);

        if (_items.Count == 0)
        {
            Close();
        }
        else
        {
            if (_currentIndex >= _items.Count)
            {
                _currentIndex = _items.Count - 1;
            }
            ShowItemAt(_currentIndex);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
