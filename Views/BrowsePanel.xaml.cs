using System.Windows.Controls.Primitives;
using Arc.ViewModels;

namespace Arc.Views;

public partial class BrowsePanel : UserControl
{
    private MainViewModel? _vm;
    private string _fileFilter = "All";

    // ── File extension filter map ─────────────────────────────────────
    private static readonly Dictionary<string, HashSet<string>> FileFilterMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["All"]       = [],
            ["Documents"] = [".doc", ".docx", ".txt", ".md", ".pptx", ".ppt", ".xlsx", ".xls", ".odt", ".rtf", ".csv"],
            ["PDFs"]      = [".pdf"],
            ["Images"]    = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".heic", ".tiff"],
            ["Videos"]    = [".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm"],
            ["Music"]     = [".mp3", ".m4a", ".flac", ".wav", ".ogg", ".aac", ".wma"],
            ["Code"]      = [".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h", ".go", ".rs", ".html", ".css", ".json", ".xml", ".yaml", ".yml"],
            ["Folders"]   = [],
        };

    public BrowsePanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // ── ViewModel wiring ─────────────────────────────────────────────

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel old)
        {
            old.PropertyChanged              -= OnVmPropertyChanged;
            old.Results.CollectionChanged    -= OnResultsChanged;
        }
        if (e.NewValue is MainViewModel vm)
        {
            _vm = vm;
            vm.PropertyChanged           += OnVmPropertyChanged;
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

    // ── Section switching + data population ──────────────────────────

    private void Refresh()
    {
        if (_vm is null) return;

        var cat = _vm.ActiveCategory;

        // Section visibility
        FilesSection.Visibility   = cat == "files"     ? Visibility.Visible : Visibility.Collapsed;
        ClipSection.Visibility    = cat == "clipboard" ? Visibility.Visible : Visibility.Collapsed;
        ActionsSection.Visibility = cat == "actions"   ? Visibility.Visible : Visibility.Collapsed;

        if (cat == "files")
        {
            _fileFilter = "All";
            UpdateFilterChipStyles();
        }

        RefreshData();
    }

    private void RefreshData()
    {
        if (_vm is null) return;

        var cat = _vm.ActiveCategory;
        if (cat is null) return;

        var results = _vm.Results.OfType<SearchResult>().ToList();

        switch (cat)
        {
            case "files":
                FilesList.ItemsSource = ApplyFileFilter(
                    results.Where(r => r.Type == ResultType.File).ToList());
                break;

            case "clipboard":
                var clips = results
                    .Where(r => r.Type == ResultType.Clipboard)
                    .ToList();
                ClipList.ItemsSource = clips;
                ClipCount.Text = $"{clips.Count} items";
                break;

            case "actions":
                ActionsList.ItemsSource = results
                    .Where(r => r.Type == ResultType.Action)
                    .ToList();
                break;
        }
    }

    private List<SearchResult> ApplyFileFilter(List<SearchResult> files)
    {
        if (_fileFilter == "All") return files;

        if (_fileFilter == "Folders")
            return files.Where(f => string.IsNullOrEmpty(f.FileExtension)).ToList();

        if (FileFilterMap.TryGetValue(_fileFilter, out var exts))
            return files.Where(f => exts.Contains(f.FileExtension ?? "")).ToList();

        return files;
    }

    // ── Filter chip click ────────────────────────────────────────────

    private void OnFilterClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        _fileFilter = btn.Tag as string ?? "All";
        UpdateFilterChipStyles();
        RefreshData();
    }

    private void UpdateFilterChipStyles()
    {
        var active   = TryFindResource("FilterChipActive") as Style;
        var inactive = TryFindResource("FilterChip")       as Style;
        if (active is null || inactive is null) return;

        foreach (var btn in new[] { FilterAll, FilterDocuments, FilterImages, FilterPDFs,
                                    FilterVideos, FilterMusic, FilterCode, FilterFolders })
        {
            btn.Style = (btn.Tag as string) == _fileFilter ? active : inactive;
        }
    }

    // Mouse hover effects

    private void OnTileMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Border b)
            b.Background = TryFindResource("HoverBg") as Brush ?? Brushes.Transparent;
    }

    private void OnTileMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Border b)
            b.Background = Brushes.Transparent;
    }

    private void OnRowMouseEnter(object sender, MouseEventArgs e)
    {
        if (GetRowBg(sender) is Border rowBg)
            rowBg.Background = TryFindResource("HoverBg") as Brush ?? Brushes.Transparent;
    }

    private void OnRowMouseLeave(object sender, MouseEventArgs e)
    {
        if (GetRowBg(sender) is Border rowBg)
            rowBg.Background = Brushes.Transparent;
    }

    // ── Item click → open ────────────────────────────────────────────

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

    // ── Helpers ──────────────────────────────────────────────────────

    private static Border? GetRowBg(object? sender)
    {
        if (sender is Grid g && g.Children.Count > 0 && g.Children[0] is Border b)
            return b;
        return null;
    }

    private void OnClearClipboard(object sender, RoutedEventArgs e)
    {
        _vm?.ClearClipboard();
        Dispatcher.InvokeAsync(RefreshData);
    }
}
