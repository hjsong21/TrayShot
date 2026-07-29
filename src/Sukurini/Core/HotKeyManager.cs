using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Sukurini.Infrastructure;

namespace Sukurini.Core;

public sealed class HotKeyManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9000;
    private const int WM_HOTKEY = 0x0312;

    private readonly IntPtr _windowHandle;
    private readonly Action _onHotKeyPressed;
    private HwndSource? _hwndSource;

    public HotKeyManager(IntPtr windowHandle, Action onHotKeyPressed)
    {
        _windowHandle = windowHandle;
        _onHotKeyPressed = onHotKeyPressed;

        _hwndSource = HwndSource.FromHwnd(_windowHandle);
        _hwndSource?.AddHook(HwndHook);
    }

    public bool Register(uint modifiers, uint key)
    {
        Unregister();
        bool success = RegisterHotKey(_windowHandle, HOTKEY_ID, modifiers, key);
        if (success) Log.App.Info($"Global hotkey registered mod={modifiers} key={key}");
        else Log.App.Warn($"Failed to register global hotkey mod={modifiers} key={key}");
        return success;
    }

    public void Unregister()
    {
        UnregisterHotKey(_windowHandle, HOTKEY_ID);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            _onHotKeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        _hwndSource?.RemoveHook(HwndHook);
        _hwndSource = null;
    }
}
