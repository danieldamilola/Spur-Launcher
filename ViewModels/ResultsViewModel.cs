using System.Collections.ObjectModel;
using System.Windows.Data;
using Spur.Models;
using Spur.Plugin;

namespace Spur.ViewModels;

public sealed partial class ResultsViewModel : ObservableObject
{
    private readonly object _lock = new();
    private List<IResultItem> _rawResults = [];

    [ObservableProperty]
    private ObservableCollection<IResultItem> _results = [];

    [ObservableProperty]
    private int _selectedIndex = -1;

    private ListCollectionView? _groupedView;

    /// <summary>Section sort order — lower numbers appear first.</summary>
    private static readonly Dictionary<string, int> SectionOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Applications"] = 0,
        ["Files"] = 1,
        ["Clipboard"] = 2,
        ["History"] = 3,
    };

    private const int DefaultSectionOrder = 99;

    /// <summary>Grouped view for the ListBox (groups by SectionName, ordered).</summary>
    public ListCollectionView? GroupedView
    {
        get => _groupedView;
        private set
        {
            if (SetProperty(ref _groupedView, value))
                OnPropertyChanged(nameof(HasResults));
        }
    }

    public bool HasResults => GroupedView is not null && GroupedView.Count > 0;

    public ResultsViewModel()
    {
        BindingOperations.EnableCollectionSynchronization(Results, _lock);
    }

    /// <summary>Replace all results (used by the scoped channel path).</summary>
    public void CommitResults(List<IResultItem> batch)
    {
        if (batch.Count == 0) return;
        _rawResults = batch;
        Results = new ObservableCollection<IResultItem>(batch);
        RebuildGroupedView();
    }

    /// <summary>Merge results from completed plugins (Flow model).</summary>
    public void AddResults(ICollection<ResultsForUpdate> updates)
    {
        if (updates.Count == 0) return;

        lock (_lock)
        {
            var pluginIds = new HashSet<string>(updates.Select(u => u.PluginId));

            var merged = _rawResults
                .OfType<SearchResult>()
                .Where(r => r.PluginId == null || !pluginIds.Contains(r.PluginId))
                .Concat(updates.SelectMany(u => u.Results))
                .OrderBy(r => GetSectionOrder(r.SectionName))
                .ThenByDescending(r => r.Score)
                .ToList();

            _rawResults = merged.Cast<IResultItem>().ToList();
            Results = new ObservableCollection<IResultItem>(_rawResults);
            if (merged.Count > 0 && SelectedIndex < 0)
                SelectedIndex = 0;
        }

        RebuildGroupedView();
    }

    public void Clear()
    {
        _rawResults.Clear();
        Results.Clear();
        SelectedIndex = -1;
        GroupedView = null;
    }

    private void RebuildGroupedView()
    {
        var view = new ListCollectionView(_rawResults)
        {
            GroupDescriptions =
            {
                new PropertyGroupDescription(nameof(SearchResult.SectionName))
            }
        };
        GroupedView = view;
    }

    private static int GetSectionOrder(string? sectionName) =>
        sectionName is not null && SectionOrder.TryGetValue(sectionName, out var order)
            ? order
            : DefaultSectionOrder;
}
