using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using TrayShot.Core;
using TrayShot.Infrastructure;

namespace TrayShot.StatusBar;

public sealed class TrayIconController : IDisposable
{
    private TaskbarIcon? _taskbarIcon;
    private TrayIconAnimator? _animator;
    private readonly Action _onToggleGallery;
    private readonly Action _onOpenPreferences;
    private readonly Action _onOpenAbout;
    private readonly Action _onExitApp;
    private bool _isDisposed;

    public TrayIconController(
        Action onToggleGallery,
        Action onOpenPreferences,
        Action onOpenAbout,
        Action onExitApp)
    {
        _onToggleGallery = onToggleGallery;
        _onOpenPreferences = onOpenPreferences;
        _onOpenAbout = onOpenAbout;
        _onExitApp = onExitApp;
    }

    public void Initialize()
    {
        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "TrayShot — 스크린샷 갤러리 & 검색",
            ContextMenu = CreateContextMenu()
        };

        _taskbarIcon.TrayLeftMouseDown += (s, e) => _onToggleGallery();

        _animator = new TrayIconAnimator(OnIconFrameRendered);

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

        var openItem = new MenuItem();
        openItem.SetResourceReference(HeaderedItemsControl.HeaderProperty, "Tray_Toggle");
        openItem.Click += (s, e) => _onToggleGallery();
        menu.Items.Add(openItem);

        menu.Items.Add(new Separator());

        var prefItem = new MenuItem();
        prefItem.SetResourceReference(HeaderedItemsControl.HeaderProperty, "Tray_Settings");
        prefItem.Click += (s, e) => _onOpenPreferences();
        menu.Items.Add(prefItem);

        var aboutItem = new MenuItem();
        aboutItem.SetResourceReference(HeaderedItemsControl.HeaderProperty, "Tray_About");
        aboutItem.Click += (s, e) => _onOpenAbout();
        menu.Items.Add(aboutItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem();
        exitItem.SetResourceReference(HeaderedItemsControl.HeaderProperty, "Tray_Exit");
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
