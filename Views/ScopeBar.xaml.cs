using Arc.ViewModels;

namespace Arc.Views;

public partial class ScopeBar
{
    public ScopeBar() => InitializeComponent();

    private void OnScopeClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is not FrameworkElement el) return;
        var id = el.Tag as string;
        if (!string.IsNullOrEmpty(id))
            vm.SetActiveScope(id);
        e.Handled = true;
    }
}
