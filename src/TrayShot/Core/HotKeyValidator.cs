using System.Collections.Generic;
using System.Windows.Input;
using TrayShot.Infrastructure;

namespace TrayShot.Core;

public record ValidationResult(bool IsValid, string Message);

public static class HotKeyValidator
{
    private static readonly HashSet<(uint Modifiers, uint KeyCode)> ReservedShortcuts = new()
    {
        // Ctrl + Letters (Standard App Controls)
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.S)), // Save
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.C)), // Copy
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.V)), // Paste
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.X)), // Cut
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.Z)), // Undo
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.Y)), // Redo
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.A)), // Select All
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.F)), // Find
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.P)), // Print
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.W)), // Close
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.N)), // New
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.O)), // Open
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.R)), // Refresh
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.T)), // New Tab
        (HotKeyManager.MOD_CONTROL, (uint)KeyInterop.VirtualKeyFromKey(Key.Q)), // Quit

        // Alt Shortcuts
        (HotKeyManager.MOD_ALT, (uint)KeyInterop.VirtualKeyFromKey(Key.F4)), // Close Window
        (HotKeyManager.MOD_ALT, (uint)KeyInterop.VirtualKeyFromKey(Key.Tab)), // Switch Window
        (HotKeyManager.MOD_ALT, (uint)KeyInterop.VirtualKeyFromKey(Key.Space)), // System Menu

        // Win Shortcuts
        (HotKeyManager.MOD_WIN, (uint)KeyInterop.VirtualKeyFromKey(Key.D)), // Desktop
        (HotKeyManager.MOD_WIN, (uint)KeyInterop.VirtualKeyFromKey(Key.E)), // Explorer
        (HotKeyManager.MOD_WIN, (uint)KeyInterop.VirtualKeyFromKey(Key.L)), // Lock
        (HotKeyManager.MOD_WIN, (uint)KeyInterop.VirtualKeyFromKey(Key.R)), // Run
        (HotKeyManager.MOD_WIN, (uint)KeyInterop.VirtualKeyFromKey(Key.S)), // Search
        (HotKeyManager.MOD_WIN | HotKeyManager.MOD_SHIFT, (uint)KeyInterop.VirtualKeyFromKey(Key.S)), // Snipping Tool
    };

    public static ValidationResult Validate(uint modifiers, uint keyCode)
    {
        string displayStr = HotKeyFormatter.Format(modifiers, keyCode);

        // 1. Check if it's a reserved standard shortcut
        if (ReservedShortcuts.Contains((modifiers, keyCode)))
        {
            return new ValidationResult(false, $"❌ 충돌: '{displayStr}'는 저장/복사/시스템 기능 등 표준 프로그램 단축키입니다. 다른 조합키(예: Ctrl+Alt+S)를 사용하세요.");
        }

        // 2. Check if it's a single modifier combination with a standard letter/digit
        bool isFKey = keyCode >= (uint)KeyInterop.VirtualKeyFromKey(Key.F1) && keyCode <= (uint)KeyInterop.VirtualKeyFromKey(Key.F24);
        int modCount = 0;
        if ((modifiers & HotKeyManager.MOD_CONTROL) != 0) modCount++;
        if ((modifiers & HotKeyManager.MOD_ALT) != 0) modCount++;
        if ((modifiers & HotKeyManager.MOD_SHIFT) != 0) modCount++;
        if ((modifiers & HotKeyManager.MOD_WIN) != 0) modCount++;

        if (modCount < 2 && !isFKey)
        {
            return new ValidationResult(false, $"❌ 충돌 위험: '{displayStr}'는 일반 앱의 기본 단축키와 충돌할 수 있습니다. 2개 이상의 조합키(예: Ctrl+Alt+S) 또는 F키를 사용해 주세요.");
        }

        // 3. Win32 System Availability check
        if (!HotKeyManager.TestAvailability(modifiers, keyCode))
        {
            return new ValidationResult(false, $"❌ 충돌: '{displayStr}'는 다른 앱 또는 Windows 시스템에서 이미 사용 중입니다. 다시 입력해 주세요.");
        }

        return new ValidationResult(true, "✓ 전역 단축키가 성공적으로 변경되었습니다.");
    }
}
