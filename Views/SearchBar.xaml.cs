using System.Windows.Controls;
using Spur.ViewModels;

namespace Spur.Views;

public partial class SearchBar : UserControl
{
    private MainViewModel? _vm;

    public SearchBar()
    {
        InitializeComponent();
        DataContextChanged += (_, e) => _vm = e.NewValue as MainViewModel;
        Loaded += (_, _) => FocusInput();
    }

    public void FocusInput()
    {
        SearchInput.Focus();
        
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (_vm?.Config.LastQueryStyle is "select" or "keep")
            {
                SearchInput.SelectAll();
            }
            else
            {
                SearchInput.CaretIndex = SearchInput.Text.Length;
            }
        });
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        Placeholder.Visibility = SearchInput.Text.Length == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
