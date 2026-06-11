using System.Windows.Controls;
using System.Windows.Input;
using Spur.ViewModels;

namespace Spur.Views;

/// <summary>
/// Command palette overlay — VS Code-style Ctrl+Shift+P.
/// </summary>
public partial class CommandPalette : UserControl
{
    public CommandPalette()
    {
        InitializeComponent();
    }

    private void OnIsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            // Focus the filter box when the palette opens
            Dispatcher.InvokeAsync(() =>
            {
                FilterBox.Focus();
                FilterBox.SelectAll();
            }, System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void OnFilterBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not CommandPaletteViewModel vm)
            return;

        switch (e.Key)
        {
            case Key.Enter:
                vm.ExecuteSelectedCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape:
                vm.CloseCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Down:
                vm.MoveSelectionCommand.Execute(1);
                e.Handled = true;
                break;

            case Key.Up:
                vm.MoveSelectionCommand.Execute(-1);
                e.Handled = true;
                break;

            case Key.Tab:
                // Cycle through results with Tab/Shift+Tab
                vm.MoveSelectionCommand.Execute(
                    Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? -1 : 1);
                e.Handled = true;
                break;
        }
    }

    private void OnClearClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not CommandPaletteViewModel vm) return;
        vm.FilterText = string.Empty;
        FilterBox.Focus();
    }
}
