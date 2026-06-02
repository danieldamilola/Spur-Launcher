using System.Windows;
using System.Windows.Controls;
using Arc.Models;
using Arc.ViewModels;

namespace Arc.Views;

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
        Loaded += OnLoaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ClipboardViewModel vm)
        {
            _vm = vm;
            vm.Refresh();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm?.Refresh();
    }

    private void OnPinClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ClipboardEntry entry })
            _vm?.TogglePinCommand.Execute(entry);
    }
}
