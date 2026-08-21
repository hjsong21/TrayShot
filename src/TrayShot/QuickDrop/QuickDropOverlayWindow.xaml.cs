using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TrayShot.Infrastructure;
using TrayShot.Models;

namespace TrayShot.QuickDrop;

public partial class QuickDropOverlayWindow : Window
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

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

        // Enforce topmost window order without stealing focus
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        if (Resources["FadeInStoryboard"] is Storyboard sb)
        {
            sb.Begin(this);
        }

        _dismissTimer.Start();
    }

    private void PositionWindow()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Top + 20;
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

                string? resolvedPath = ResolveCurrentFilePath(_screenshot.Path);
                if (string.IsNullOrEmpty(resolvedPath))
                {
                    Log.App.Warn($"QuickDrop drag aborted: file no longer exists ({_screenshot.Path})");
                    Close();
                    return;
                }

                try
                {
                    var dataObj = DragDropHelper.CreateDragDataObject(resolvedPath);
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

    /// <summary>
    /// 드래그 시작 시점에 실제로 디스크에 존재하는 파일 경로를 확정합니다.
    /// 백그라운드 변환으로 원본 PNG가 이미 처분(Delete/Trash)된 경우 동일 이름의 WebP로 안전하게 폴백합니다.
    /// </summary>
    private static string? ResolveCurrentFilePath(string originalPath)
    {
        if (!string.IsNullOrEmpty(originalPath) && File.Exists(originalPath))
            return originalPath;

        // 원본 PNG가 삭제된 경우 동일 이름의 WebP 파일 확인
        string webpPath = ScreenshotFile.ConvertedPath(originalPath);
        if (File.Exists(webpPath))
        {
            Log.App.Info($"QuickDrop: original PNG gone, falling back to WebP: {webpPath}");
            return webpPath;
        }

        return null;
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
