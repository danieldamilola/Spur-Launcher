using System.Windows.Controls.Primitives;
using Arc.ViewModels;

namespace Arc.Views;

/// <summary>DataTemplateSelector that routes SectionLabel vs SearchResult.</summary>
public sealed class ResultTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SectionTemplate { get; set; }
    public DataTemplate? ResultTemplate  { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is SectionLabel ? SectionTemplate : ResultTemplate;
}

public partial class ResultsList : UserControl
{
    public ResultsList()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel old)
            old.PropertyChanged -= OnVmChanged;
        if (e.NewValue is MainViewModel vm)
            vm.PropertyChanged += OnVmChanged;
    }

    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedIndex))
            Dispatcher.InvokeAsync(ScrollToSelected);
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        => ScrollToSelected();

    private void ScrollToSelected()
    {
        if (DataContext is not MainViewModel vm) return;
        int idx = vm.SelectedIndex;
        if (idx < 0 || idx >= vm.Results.Count) return;

        var container = ResultsBox.ItemContainerGenerator.ContainerFromIndex(idx) as FrameworkElement;
        container?.BringIntoView();
    }

    private void OnRowClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is not FrameworkElement el) return;
        if (el.DataContext is not SearchResult result) return;

        int idx = vm.Results.IndexOf(result);
        if (idx >= 0)
        {
            vm.SelectedIndex = idx;
            vm.OpenSelectedCommand.Execute(null);
        }
    }
}

