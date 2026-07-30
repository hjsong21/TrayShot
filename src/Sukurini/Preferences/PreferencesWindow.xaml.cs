using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Sukurini.Core;

namespace Sukurini.Preferences;

public partial class PreferencesWindow : Window
{
    public PreferencesWindow()
    {
        InitializeComponent();
        DataContext = new PreferencesViewModel();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void HotKeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore standalone modifier key presses
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        uint modifiers = 0;
        var wpfMods = Keyboard.Modifiers;
        if ((wpfMods & ModifierKeys.Alt) != 0) modifiers |= HotKeyManager.MOD_ALT;
        if ((wpfMods & ModifierKeys.Control) != 0) modifiers |= HotKeyManager.MOD_CONTROL;
        if ((wpfMods & ModifierKeys.Shift) != 0) modifiers |= HotKeyManager.MOD_SHIFT;
        if ((wpfMods & ModifierKeys.Windows) != 0) modifiers |= HotKeyManager.MOD_WIN;

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);

        if (DataContext is PreferencesViewModel vm)
        {
            vm.TrySetHotKey(modifiers, vk);
        }
    }

    private void HotKeyTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
        {
            tb.SelectAll();
        }
    }
}
