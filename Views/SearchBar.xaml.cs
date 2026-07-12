using System.Windows.Controls;
using Spur.ViewModels;

namespace Spur.Views;

public partial class SearchBar : UserControl
{
    private MainViewModel? _vm;

    public SearchBar()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => FocusInput();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _vm = e.NewValue as MainViewModel;
    }

    public void FocusInput()
    {
        SearchInput.Focus();
        if (_vm is null) return;

        if (_vm.Config.LastQueryStyle is "select" or "keep")
            SearchInput.SelectAll();
        else
            SearchInput.CaretIndex = SearchInput.Text.Length;
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        Placeholder.Visibility = SearchInput.Text.Length == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnClipboardCloseClick(object sender, RoutedEventArgs e)
    {
        if (_vm is not null)
            _vm.ActiveCategory = null;
    }

    private void OnClipMoreClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.ContextMenu is not null)
        {
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            btn.ContextMenu.IsOpen = true;
        }
    }
}
