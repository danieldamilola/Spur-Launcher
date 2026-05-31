using Arc.ViewModels;
using System.Windows;

namespace Arc.Views;

/// <summary>
/// Settings window — thin host shell.
/// Navigation and layout are owned entirely by SettingsView (UserControl).
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

    /// <summary>Hide instead of close. Window persists for re-show.</summary>
    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
