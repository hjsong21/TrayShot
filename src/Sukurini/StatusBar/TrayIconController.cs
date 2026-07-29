using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using Sukurini.Core;
using Sukurini.Infrastructure;

namespace Sukurini.StatusBar;

public sealed class TrayIconController : IDisposable
{
    private TaskbarIcon? _taskbarIcon;
    private TrayIconAnimator? _animator;
    private readonly Action _onToggleGallery;
    private readonly Action _onOpenPreferences;
    private readonly Action _onExitApp;
    private bool _isDisposed;

    public TrayIconController(
        Action onToggleGallery,
        Action onOpenPreferences,
        Action onExitApp)
    {
        _onToggleGallery = onToggleGallery;
        _onOpenPreferences = onOpenPreferences;
        _onExitApp = onExitApp;
    }

    public void Initialize()
    {
        _animator = new TrayIconAnimator(OnIconFrameRendered);

        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "Sukurini — 스크린샷 갤러리 & 검색",
            ContextMenu = CreateContextMenu()
        };

        _taskbarIcon.TrayLeftMouseDown += (s, e) => _onToggleGallery();

        // ScreenshotStore 새 스크린샷 등록 시 애니메이션 실행
        ScreenshotStore.Shared.Changed += change =>
        {
            if (change.Inserted.Count > 0)
            {
                _animator.PlayPulse();
            }
        };

        Log.App.Info("TrayIconController initialized");
    }

    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        var openItem = new MenuItem { Header = "갤러리 열기 / 닫기" };
        openItem.Click += (s, e) => _onToggleGallery();
        menu.Items.Add(openItem);

        menu.Items.Add(new Separator());

        var prefItem = new MenuItem { Header = "설정..." };
        prefItem.Click += (s, e) => _onOpenPreferences();
        menu.Items.Add(prefItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "종료" };
        exitItem.Click += (s, e) => _onExitApp();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void OnIconFrameRendered(Icon icon)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (_taskbarIcon != null && !_isDisposed)
            {
                _taskbarIcon.Icon = icon;
            }
        });
    }

    public void PlayPulse() => _animator?.PlayPulse();
    public void PlayBounce() => _animator?.PlayBounce();

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _animator?.Dispose();
        if (_taskbarIcon != null)
        {
            _taskbarIcon.Dispose();
            _taskbarIcon = null;
        }
    }
}
