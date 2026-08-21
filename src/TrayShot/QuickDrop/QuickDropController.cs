using System;
using System.Linq;
using System.Windows;
using TrayShot.Core;
using TrayShot.Infrastructure;
using TrayShot.Models;

namespace TrayShot.QuickDrop;

public sealed class QuickDropController : IDisposable
{
    private QuickDropOverlayWindow? _activeOverlay;
    private readonly Action<Screenshot> _onOpenGalleryWithScreenshot;
    private bool _isDisposed;

    public QuickDropController(Action<Screenshot> onOpenGalleryWithScreenshot)
    {
        _onOpenGalleryWithScreenshot = onOpenGalleryWithScreenshot;
    }

    public void Initialize()
    {
        ScreenshotStore.Shared.Changed += OnStoreChanged;
        Log.App.Info("QuickDropController initialized");
    }

    private void OnStoreChanged(StoreChange change)
    {
        if (_isDisposed || change.Inserted.Count == 0) return;

        // WebP 파일 등 포맷 교체(Replacements)로 생성된 항목은 오버레이 표시 대상에서 제외
        var overlayTargets = change.Inserted
            .Where(f => !change.Replacements.Values.Contains(f.Path, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (overlayTargets.Count == 0) return;

        var latestScreenshot = overlayTargets[0];

        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            if (_isDisposed) return;

            try
            {
                // Close previous overlay if still open
                if (_activeOverlay != null && _activeOverlay.IsLoaded)
                {
                    _activeOverlay.Close();
                    _activeOverlay = null;
                }

                _activeOverlay = new QuickDropOverlayWindow(latestScreenshot);
                _activeOverlay.OpenGalleryRequested += screenshot =>
                {
                    _onOpenGalleryWithScreenshot(screenshot);
                };
                _activeOverlay.Closed += (s, e) =>
                {
                    if (ReferenceEquals(_activeOverlay, s))
                    {
                        _activeOverlay = null;
                    }
                };

                _activeOverlay.Show();
            }
            catch (Exception ex)
            {
                Log.App.Error($"Failed to show QuickDrop overlay: {ex.Message}");
            }
        });
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        ScreenshotStore.Shared.Changed -= OnStoreChanged;

        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (_activeOverlay != null && _activeOverlay.IsLoaded)
            {
                _activeOverlay.Close();
                _activeOverlay = null;
            }
        });
    }
}
