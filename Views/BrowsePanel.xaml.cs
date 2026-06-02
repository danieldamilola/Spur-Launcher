using System.Windows.Controls.Primitives;
using Arc.Models;
using Arc.ViewModels;

namespace Arc.Views;

public partial class BrowsePanel : UserControl
{
    private MainViewModel? _vm;

    public BrowsePanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel old)
        {
            old.PropertyChanged -= OnVmPropertyChanged;
            old.Results.CollectionChanged -= OnResultsChanged;
        }
        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
            vm.PropertyChanged += OnVmPropertyChanged;
            vm.Results.CollectionChanged += OnResultsChanged;
            Dispatcher.InvokeAsync(Refresh);
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.ActiveCategory)
                           or nameof(MainViewModel.CatalogLoading))
            Dispatcher.InvokeAsync(Refresh);
    }

    private void OnResultsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => Dispatcher.InvokeAsync(RefreshData);

    private void Refresh()
    {
        if (_vm is null) return;

        var cat = _vm.ActiveCategory;

        FilesSection.Visibility   = cat == "files"     ? Visibility.Visible : Visibility.Collapsed;
        ClipSection.Visibility    = cat == "clipboard" ? Visibility.Visible : Visibility.Collapsed;
        ActionsSection.Visibility = cat == "actions"   ? Visibility.Visible : Visibility.Collapsed;

        RefreshData();
    }

    // TODO: Replace with XAML bindings when MainViewModel exposes per-category collections
    private void RefreshData()
    {
        if (_vm is null) return;

        var cat = _vm.ActiveCategory;
        if (cat is null) return;

        var results = _vm.Results.OfType<SearchResult>().ToList();

        switch (cat)
        {
            case "files":
                FilesList.ItemsSource = results.Where(r => r.Type == ResultType.File).Take(50).ToList();
                break;

            case "clipboard":
                var clips = results.Where(r => r.Type == ResultType.Clipboard).ToList();
                ClipList.ItemsSource = clips;
                ClipCount.Text = $"{clips.Count} items";
                break;

            case "actions":
                ActionsList.ItemsSource = results.Where(r => r.Type == ResultType.Action).ToList();
                break;
        }
    }

    private void OnItemClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm is null) return;
        if (sender is not FrameworkElement el) return;
        if (el.DataContext is not SearchResult item) return;

        int idx = _vm.Results.IndexOf(item);
        if (idx >= 0)
        {
            _vm.SelectedIndex = idx;
            _vm.OpenSelectedCommand.Execute(null);
        }
    }

    private void OnClearClipboard(object sender, RoutedEventArgs e)
    {
        _vm?.ClearClipboard();
        Dispatcher.InvokeAsync(RefreshData);
    }
}
