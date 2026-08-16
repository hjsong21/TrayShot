using System.Collections.Generic;
using System.Windows.Input;

namespace TrayShot.Core;

public static class HotKeyFormatter
{
    public static string Format(uint modifiers, uint keyCode)
    {
        List<string> parts = new();

        if ((modifiers & HotKeyManager.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & HotKeyManager.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & HotKeyManager.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & HotKeyManager.MOD_WIN) != 0) parts.Add("Win");

        string keyName = GetKeyName(keyCode);
        if (!string.IsNullOrEmpty(keyName))
        {
            parts.Add(keyName);
        }

        return parts.Count > 0 ? string.Join(" + ", parts) : "없음";
    }

    private static string GetKeyName(uint keyCode)
    {
        Key key = KeyInterop.KeyFromVirtualKey((int)keyCode);
        return key switch
        {
            Key.None => "",
            Key.D0 => "0",
            Key.D1 => "1",
            Key.D2 => "2",
            Key.D3 => "3",
            Key.D4 => "4",
            Key.D5 => "5",
            Key.D6 => "6",
            Key.D7 => "7",
            Key.D8 => "8",
            Key.D9 => "9",
            Key.OemQuestion => "/",
            Key.OemQuotes => "'",
            Key.OemPlus => "+",
            Key.OemMinus => "-",
            Key.OemPeriod => ".",
            Key.OemComma => ",",
            Key.Oem1 => ";",
            Key.Oem3 => "`",
            Key.OemOpenBrackets => "[",
            Key.Oem6 => "]",
            Key.Oem5 => "\\",
            _ => key.ToString()
        };
    }
}
