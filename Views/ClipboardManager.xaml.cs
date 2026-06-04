using System.Windows;
using System.Windows.Controls;
using Spur.Models;
using Spur.ViewModels;

namespace Spur.Views;

/// <summary>
/// Code-behind for ClipboardManager.xaml. Refreshes the ViewModel on load
/// and handles the pin button click (event-based, not command-based).
/// </summary>
public partial class ClipboardManager : UserControl
{
    private ClipboardViewModel? _vm;

    public ClipboardManager()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ClipboardViewModel vm)
        {
            _vm = vm;
            vm.Refresh();
        }
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            _vm?.Refresh();
        }
    }

    private void OnPinClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ClipboardEntry entry })
            _vm?.TogglePinCommand.Execute(entry);
    }
}
