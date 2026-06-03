using Spur.ViewModels;

namespace Spur.Views;

public partial class ScopeBar
{
    public ScopeBar() => InitializeComponent();

    private void OnScopeClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is not Button btn) return;
        var id = btn.Tag as string;
        if (!string.IsNullOrEmpty(id))
            vm.SetActiveScope(id);
        e.Handled = true;
    }
}
