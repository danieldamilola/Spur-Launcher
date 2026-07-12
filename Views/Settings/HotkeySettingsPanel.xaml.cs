using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Spur.ViewModels;

namespace Spur.Views.Settings;

public partial class HotkeySettingsPanel : UserControl
{
    private bool _recordingShortcut;
    private Button? _recordingButton;
    private string? _recordingTarget;

    public HotkeySettingsPanel()
    {
        InitializeComponent();
    }

    private void OnEditShortcutClick(object sender, RoutedEventArgs e) => StartRecording(sender, "Shortcut");
    private void OnEditClipboardShortcutClick(object sender, RoutedEventArgs e) => StartRecording(sender, "ClipboardShortcut");
    private void OnGenericEdit(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string prop)
            StartRecording(sender, prop);
    }

    private void StartRecording(object sender, string target)
    {
        if (_recordingShortcut) return;
        _recordingShortcut = true;
        _recordingTarget = target;
        if (sender is Button btn)
        {
            _recordingButton = btn;
            btn.Content = "Recording…";
            if (TryFindResource("TextPrimary") is System.Windows.Media.Brush b)
                btn.Foreground = b;
            Keyboard.Focus(btn);
            btn.PreviewKeyDown += OnShortcutKeyDown;
            btn.LostFocus      += OnShortcutLostFocus;
        }
    }

    private void OnShortcutKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                 or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        var mods = Keyboard.Modifiers;
        var parts = new System.Collections.Generic.List<string>();
        if ((mods & ModifierKeys.Control) != 0) parts.Add("Ctrl");
        if ((mods & ModifierKeys.Alt)     != 0) parts.Add("Alt");
        if ((mods & ModifierKeys.Shift)   != 0) parts.Add("Shift");
        if ((mods & ModifierKeys.Windows) != 0) parts.Add("Win");
        parts.Add(key.ToString());

        if (DataContext is SettingsViewModel vm && _recordingTarget is not null)
        {
            var shortcutValue = string.Join("+", parts);
            var prop = typeof(SettingsViewModel).GetProperty(_recordingTarget);
            prop?.SetValue(vm, shortcutValue);
        }

        StopRecording();
    }

    private void OnShortcutLostFocus(object sender, RoutedEventArgs e) => StopRecording();

    private void StopRecording()
    {
        if (!_recordingShortcut) return;
        _recordingShortcut = false;
        _recordingTarget = null;
        if (_recordingButton is Button btn)
        {
            btn.Content = "Edit";
            btn.PreviewKeyDown -= OnShortcutKeyDown;
            btn.LostFocus      -= OnShortcutLostFocus;
            if (TryFindResource("TextSecondary") is System.Windows.Media.Brush b)
                btn.Foreground = b;
            _recordingButton = null;
        }
    }
}
