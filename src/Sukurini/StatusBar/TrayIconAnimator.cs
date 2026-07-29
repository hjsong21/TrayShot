using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Threading;
using Sukurini.Infrastructure;

namespace Sukurini.StatusBar;

public enum AnimationState
{
    Idle,
    Pulse,
    Bounce
}

public sealed class TrayIconAnimator : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Action<Icon> _onFrameRendered;

    private AnimationState _currentState = AnimationState.Idle;
    private double _animationProgress = 0;
    private int _pulseCount = 0;
    private bool _isDisposed;

    public AnimationState CurrentState => _currentState;

    public TrayIconAnimator(Action<Icon> onFrameRendered)
    {
        _onFrameRendered = onFrameRendered;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
        };
        _timer.Tick += OnTimerTick;

        // Render initial idle icon
        RenderCurrentFrame();
    }

    public void PlayPulse()
    {
        _currentState = AnimationState.Pulse;
        _animationProgress = 0;
        _pulseCount = 0;
        if (!_timer.IsEnabled) _timer.Start();
        Log.App.Debug("TrayIconAnimator started pulse animation");
    }

    public void PlayBounce()
    {
        _currentState = AnimationState.Bounce;
        _animationProgress = 0;
        if (!_timer.IsEnabled) _timer.Start();
        Log.App.Debug("TrayIconAnimator started bounce animation");
    }

    public void Stop()
    {
        _currentState = AnimationState.Idle;
        _timer.Stop();
        RenderCurrentFrame();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _animationProgress += 0.05;

        if (_currentState == AnimationState.Pulse)
        {
            if (_animationProgress >= 1.0)
            {
                _animationProgress = 0;
                _pulseCount++;
                if (_pulseCount >= 3)
                {
                    Stop();
                    return;
                }
            }
        }
        else if (_currentState == AnimationState.Bounce)
        {
            if (_animationProgress >= 1.0)
            {
                Stop();
                return;
            }
        }

        RenderCurrentFrame();
    }

    private void RenderCurrentFrame()
    {
        try
        {
            using var bitmap = new Bitmap(32, 32);
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Base circle
            using var baseBrush = new SolidBrush(Color.FromArgb(255, 60, 120, 240));
            g.FillEllipse(baseBrush, 4, 4, 24, 24);

            if (_currentState == AnimationState.Pulse)
            {
                // Expanding ring effect
                int ringAlpha = (int)(255 * (1.0 - _animationProgress));
                int ringRadius = (int)(12 + (_animationProgress * 10));
                int ringOffset = 16 - ringRadius;
                int ringDiameter = ringRadius * 2;

                using var ringPen = new Pen(Color.FromArgb(ringAlpha, 60, 120, 240), 2);
                if (ringOffset >= 0 && ringDiameter > 0 && ringDiameter <= 32)
                {
                    g.DrawEllipse(ringPen, ringOffset, ringOffset, ringDiameter, ringDiameter);
                }
            }
            else if (_currentState == AnimationState.Bounce)
            {
                // Spring scale effect
                double scale = 1.0 + Math.Sin(_animationProgress * Math.PI) * 0.3;
                int size = (int)(24 * scale);
                int offset = 16 - (size / 2);

                using var bounceBrush = new SolidBrush(Color.FromArgb(255, 90, 150, 255));
                g.FillEllipse(bounceBrush, offset, offset, size, size);
            }

            // Inner camera lens indicator
            using var innerBrush = new SolidBrush(Color.White);
            g.FillEllipse(innerBrush, 12, 12, 8, 8);

            IntPtr hIcon = bitmap.GetHicon();
            using var icon = Icon.FromHandle(hIcon);
            _onFrameRendered(icon);
        }
        catch (Exception ex)
        {
            Log.App.Error($"RenderCurrentFrame error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _timer.Stop();
    }
}
