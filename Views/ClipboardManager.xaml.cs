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
        if (e.OldValue is ClipboardViewModel oldVm)
            oldVm.PropertyChanged -= OnVmPropertyChanged;

        if (e.NewValue is ClipboardViewModel vm)
        {
            _vm = vm;
            vm.PropertyChanged += OnVmPropertyChanged;
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ClipboardViewModel.SelectedEntry))
            Dispatcher.InvokeAsync(UpdateDetailPinState);
    }

    /// <summary>Syncs the detail panel pin button glyph with the selected entry's pinned state.</summary>
    private void UpdateDetailPinState()
    {
        var entry = _vm?.SelectedEntry;
        bool isPinned = entry?.IsPinned == true;
        DetailPinGlyph.Glyph = isPinned ? "\uE841" : "\uE718";
        DetailPinBtn.ToolTip = isPinned ? "Unpin" : "Pin";
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
        {
            _vm?.TogglePinCommand.Execute(entry);
            // Refresh the detail pin state after toggling
            Dispatcher.InvokeAsync(UpdateDetailPinState);
        }
    }

    public void MoveSelection(int delta) => _vm?.MoveSelection(delta);
}
