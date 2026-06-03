using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Arc.Models;
using Arc.Services;

namespace Arc.ViewModels;

/// <summary>
/// ViewModel for the Ctrl+Shift+P command palette overlay.
/// </summary>
public sealed partial class CommandPaletteViewModel : ObservableObject
{
    private readonly ICommandRegistry _registry;

    public CommandPaletteViewModel(ICommandRegistry registry)
    {
        _registry = registry;
    }

    // ── Observable state ───────────────────────────────────────────

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private int _selectedIndex = -1;

    public ObservableCollection<CommandPaletteItem> FilteredCommands { get; } = [];

    [ObservableProperty]
    private bool _showNoMatch;

    [ObservableProperty]
    private bool _hasFilter;

    partial void OnFilterTextChanged(string value)
    {
        RebuildFilter();
        HasFilter = !string.IsNullOrEmpty(value);
        SelectedIndex = FilteredCommands.Count > 0 ? 0 : -1;
        ShowNoMatch = HasFilter && FilteredCommands.Count == 0;
    }

    partial void OnIsOpenChanged(bool value)
    {
        if (value)
        {
            FilterText = string.Empty;
            HasFilter = false;
            ShowNoMatch = false;
            RebuildFilter();
            SelectedIndex = FilteredCommands.Count > 0 ? 0 : -1;
        }
    }

    // ── Commands ───────────────────────────────────────────────────

    [RelayCommand]
    private void ExecuteSelected()
    {
        if (SelectedIndex < 0 || SelectedIndex >= FilteredCommands.Count)
            return;
        var item = FilteredCommands[SelectedIndex];
        item.ExecuteAction?.Invoke();
        IsOpen = false;
    }

    [RelayCommand]
    private void MoveSelection(int direction)
    {
        if (FilteredCommands.Count == 0)
        {
            SelectedIndex = -1;
            return;
        }

        int next = SelectedIndex + direction;
        if (next < 0) next = FilteredCommands.Count - 1;
        if (next >= FilteredCommands.Count) next = 0;
        SelectedIndex = next;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    // ── Internal ───────────────────────────────────────────────────

    private void RebuildFilter()
    {
        FilteredCommands.Clear();

        IEnumerable<CommandPaletteEntry> entries;
        if (string.IsNullOrWhiteSpace(FilterText))
            entries = _registry.All;
        else
            entries = _registry.Search(FilterText);

        foreach (var entry in entries)
        {
            FilteredCommands.Add(new CommandPaletteItem
            {
                Id = entry.Id,
                Label = entry.Label,
                Description = entry.Description,
                LucideIcon = entry.LucideIcon,
                ExecuteAction = entry.Execute,
            });
        }
    }
}
