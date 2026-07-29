using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Sukurini.Infrastructure;

namespace Sukurini.Core;

public sealed class HotKeyManager : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    private const int HOTKEY_ID = 9000;
    private const int WM_HOTKEY = 0x0312;

    private readonly IntPtr _windowHandle;
    private readonly HwndSource? _hwndSource;
    private readonly Action _onHotKeyPressed;
    private bool _isRegistered;
    private DateTime _lastTriggerTime = DateTime.MinValue;

    public HotKeyManager(IntPtr windowHandle, Action onHotKeyPressed)
    {
        _windowHandle = windowHandle;
        _onHotKeyPressed = onHotKeyPressed;

        if (_windowHandle != IntPtr.Zero)
        {
            _hwndSource = HwndSource.FromHwnd(_windowHandle);
            _hwndSource?.AddHook(HwndHook);
        }

        ComponentDispatcher.ThreadFilterMessage += OnThreadFilterMessage;
    }

    public bool Register(uint modifiers, uint key)
    {
        Unregister();

        uint flags = modifiers | MOD_NOREPEAT;
        IntPtr targetHwnd = _windowHandle != IntPtr.Zero ? _windowHandle : IntPtr.Zero;

        bool success = RegisterHotKey(targetHwnd, HOTKEY_ID, flags, key);
        if (success)
        {
            _isRegistered = true;
            Log.App.Info($"Global hotkey registered successfully: mod=0x{modifiers:X} key=0x{key:X} hwnd=0x{targetHwnd:X}");
        }
        else
        {
            int err = Marshal.GetLastWin32Error();
            Log.App.Warn($"Failed to register hotkey with hwnd=0x{targetHwnd:X} (err={err}). Retrying fallback...");

            // Fallback 1: Try without MOD_NOREPEAT
            success = RegisterHotKey(targetHwnd, HOTKEY_ID, modifiers, key);
            if (!success && targetHwnd != IntPtr.Zero)
            {
                // Fallback 2: Try thread-wide (IntPtr.Zero)
                success = RegisterHotKey(IntPtr.Zero, HOTKEY_ID, flags, key);
            }

            if (success)
            {
                _isRegistered = true;
                Log.App.Info($"Global hotkey registered on fallback: mod=0x{modifiers:X} key=0x{key:X}");
            }
            else
            {
                int errFinal = Marshal.GetLastWin32Error();
                Log.App.Error($"Global hotkey registration completely failed: mod=0x{modifiers:X} key=0x{key:X} (Win32Error={errFinal})");
            }
        }
        return success;
    }

    public static bool TestAvailability(uint modifiers, uint key)
    {
        var current = AppSettings.Shared.GalleryHotKey;
        if (current.Modifiers == modifiers && current.KeyCode == key)
        {
            return true;
        }

        const int TEST_ID = 9998;
        uint flags = modifiers | MOD_NOREPEAT;

        bool success = RegisterHotKey(IntPtr.Zero, TEST_ID, flags, key);
        if (success)
        {
            UnregisterHotKey(IntPtr.Zero, TEST_ID);
            return true;
        }

        success = RegisterHotKey(IntPtr.Zero, TEST_ID, modifiers, key);
        if (success)
        {
            UnregisterHotKey(IntPtr.Zero, TEST_ID);
            return true;
        }

        return false;
    }

    public void Unregister()
    {
        if (_isRegistered)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            UnregisterHotKey(IntPtr.Zero, HOTKEY_ID);
            _isRegistered = false;
        }
    }

    private void TriggerHotKey()
    {
        if ((DateTime.UtcNow - _lastTriggerTime).TotalMilliseconds < 200)
            return; // Debounce double triggers

        _lastTriggerTime = DateTime.UtcNow;
        _onHotKeyPressed?.Invoke();
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            Log.App.Info("Global Hotkey intercepted by HwndHook!");
            TriggerHotKey();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void OnThreadFilterMessage(ref MSG msg, ref bool handled)
    {
        if (msg.message == WM_HOTKEY && msg.wParam.ToInt32() == HOTKEY_ID)
        {
            Log.App.Info("Global Hotkey message intercepted by ThreadFilterMessage!");
            TriggerHotKey();
            handled = true;
        }
    }

    public void Dispose()
    {
        Unregister();
        _hwndSource?.RemoveHook(HwndHook);
        ComponentDispatcher.ThreadFilterMessage -= OnThreadFilterMessage;
    }
}
