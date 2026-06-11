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
        }
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible && _vm is not null)
        {
            _vm.Refresh();
        }
    }

    private void OnListMouseClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject src)
        {
            var item = ItemsControl.ContainerFromElement(ClipList, src) as ListBoxItem;
            if (item?.DataContext is ClipboardEntry entry && _vm != null)
            {
                _vm.SelectedEntry = entry;
                _vm.CopyCommand.Execute(entry);
                e.Handled = true;
            }
        }
    }

    private void OnPinClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: ClipboardEntry entry })
            _vm?.TogglePinCommand.Execute(entry);
    }

    public void MoveSelection(int delta) => _vm?.MoveSelection(delta);
}
