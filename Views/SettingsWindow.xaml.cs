using Spur.ViewModels;
using System.Windows;

namespace Spur.Views;

/// <summary>
/// Settings window — custom-chrome host shell.
/// Navigation and layout are owned entirely by SettingsView.
/// Hides on close so it can be reopened instantly.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    public void SetViewModel(SettingsViewModel vm)
    {
        SettingsViewControl.DataContext = vm;
    }

    // ── Custom title-bar button handlers ─────────────────────────────────

    private void OnMinimiseClick(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaximiseClick(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        => Hide();

    /// <summary>Hide instead of close — window persists for re-show.</summary>
    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
