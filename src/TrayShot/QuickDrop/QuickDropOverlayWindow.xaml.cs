using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TrayShot.Infrastructure;
using TrayShot.Models;

namespace TrayShot.QuickDrop;

public partial class QuickDropOverlayWindow : Window
{
    private readonly Screenshot _screenshot;
    private readonly DispatcherTimer _dismissTimer;
    private Point _dragStartPoint;
    private bool _isDragging;
    private bool _isClosing;

    public event Action<Screenshot>? OpenGalleryRequested;

    public QuickDropOverlayWindow(Screenshot screenshot)
    {
        InitializeComponent();
        _screenshot = screenshot;

        _dismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(4)
        };
        _dismissTimer.Tick += (s, e) => CloseWithFadeOut();

        Loaded += OnLoaded;
        MouseEnter += (s, e) => _dismissTimer.Stop();
        MouseLeave += (s, e) =>
        {
            if (!_isClosing && !_isDragging)
            {
                _dismissTimer.Interval = TimeSpan.FromSeconds(2);
                _dismissTimer.Start();
            }
        };

        PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        PreviewMouseMove += OnPreviewMouseMove;
        PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
        LoadThumbnail();

        if (Resources["FadeInStoryboard"] is Storyboard sb)
        {
            sb.Begin(this);
        }

        _dismissTimer.Start();
    }

    private void PositionWindow()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 16;
        Top = workArea.Bottom - Height - 16;
    }

    private void LoadThumbnail()
    {
        if (string.IsNullOrEmpty(_screenshot.Path) || !File.Exists(_screenshot.Path)) return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 320;
            bitmap.UriSource = new Uri(_screenshot.Path);
            bitmap.EndInit();
            bitmap.Freeze();

            ThumbnailImage.Source = bitmap;
        }
        catch (Exception ex)
        {
            Log.App.Warn($"Failed to load quick-drop thumbnail: {ex.Message}");
        }
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        _isDragging = false;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isDragging && !_isClosing)
        {
            Point currentPos = e.GetPosition(this);
            Vector diff = _dragStartPoint - currentPos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isDragging = true;
                _dismissTimer.Stop();

                string path = _screenshot.Path;
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

                try
                {
                    var dataObj = DragDropHelper.CreateDragDataObject(path);
                    DragDrop.DoDragDrop(this, dataObj, DragDropEffects.Copy);
                }
                catch (Exception ex)
                {
                    Log.App.Error($"QuickDrop drag failed: {ex.Message}");
                }
                finally
                {
                    // Once dragged and dropped, close the overlay immediately
                    Close();
                }
            }
        }
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // If user clicked without dragging (and didn't click close button)
        if (!_isDragging && !_isClosing)
        {
            Point currentPos = e.GetPosition(this);
            Vector diff = _dragStartPoint - currentPos;

            if (Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance)
            {
                OpenGalleryRequested?.Invoke(_screenshot);
                CloseWithFadeOut();
            }
        }
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        CloseWithFadeOut();
    }

    private void CloseWithFadeOut()
    {
        if (_isClosing) return;
        _isClosing = true;
        _dismissTimer.Stop();

        if (Resources["FadeOutStoryboard"] is Storyboard sb)
        {
            sb.Completed += (s, e) => Close();
            sb.Begin(this);
        }
        else
        {
            Close();
        }
    }
}
