using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Volt.ViewModels;

namespace Volt.Views;

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
        SearchInput.CaretIndex = SearchInput.Text.Length;
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        Placeholder.Visibility = SearchInput.Text.Length == 0
            ? Visibility.Visible : Visibility.Collapsed;
    }
}
