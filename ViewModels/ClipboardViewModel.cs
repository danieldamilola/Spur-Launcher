using System.Collections.ObjectModel;
using Arc.Models;
using Arc.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arc.ViewModels;

/// <summary>
/// Standalone clipboard manager ViewModel. Extracted from the launcher's main
/// search flow so clipboard browsing, copy, delete, and pinning are self-contained.
/// </summary>
public sealed partial class ClipboardViewModel : ObservableObject
{
    private readonly IClipboardService _clipboard;
    private readonly ArcConfig _config;
    private readonly IConfigService _configSvc;

    public ClipboardViewModel(
        IClipboardService clipboard,
        ArcConfig config,
        IConfigService configSvc)
    {
        _clipboard = clipboard;
        _config = config;
        _configSvc = configSvc;
    }

    // ── Observable properties ──────────────────────────────────────

    public ObservableCollection<ClipboardEntry> Entries { get; } = [];

    [ObservableProperty]
    private string _filterText = string.Empty;

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    [ObservableProperty]
    private ClipboardEntry? _selectedEntry;

    partial void OnSelectedEntryChanged(ClipboardEntry? value)
    {
        OnPropertyChanged(nameof(SelectedContent));
        IsImageSelected = value?.IsImage ?? false;
    }

    /// <summary>Full text of the selected entry (shown in detail area).</summary>
    public string? SelectedContent => SelectedEntry?.Content;

    [ObservableProperty]
    private bool _isImageSelected;

    public int TotalCount => _clipboard.GetHistory().Count;

    public int FilteredCount => Entries.Count;

    [ObservableProperty]
    private string _statusText = string.Empty;

    // ── Public API ──────────────────────────────────────────────────

    /// <summary>Refreshes the entry list from the clipboard service.</summary>
    public void Refresh()
    {
        var history = _clipboard.GetHistory();
        Entries.Clear();
        foreach (var entry in history)
            Entries.Add(entry);

        ApplyFilter();
        UpdateStatus();
    }

    /// <summary>Copy a single entry to the system clipboard.</summary>
    [RelayCommand]
    public void Copy(ClipboardEntry? entry)
    {
        if (entry is null) return;
        _clipboard.CopyToSystem(entry.Content);
        StatusText = "Copied";
    }

    /// <summary>Remove a single entry from history.</summary>
    [RelayCommand]
    public void Delete(ClipboardEntry? entry)
    {
        if (entry is null) return;
        var keep = _clipboard.GetHistory()
            .Where(e => e.Content != entry.Content)
            .Select(e => e.Content)
            .ToHashSet();
        _clipboard.KeepOnly(keep);
        Refresh();
    }

    /// <summary>Clear all non-pinned entries.</summary>
    [RelayCommand]
    public void ClearAll()
    {
        var pinned = _config.PinnedClipboard.Select(p => p.Content).ToHashSet();
        _clipboard.KeepOnly(pinned);
        Refresh();
    }

    /// <summary>Toggle pin status for an entry.</summary>
    [RelayCommand]
    public void TogglePin(ClipboardEntry? entry)
    {
        if (entry is null) return;
        var existing = _config.PinnedClipboard.FirstOrDefault(p => p.Content == entry.Content);
        if (existing is not null)
            _config.PinnedClipboard.Remove(existing);
        else
            _config.PinnedClipboard.Add(new PinnedClipboardItem
            {
                Id = $"clip:{entry.Timestamp.Ticks}",
                Content = entry.Content,
                Preview = entry.Preview,
                Timestamp = entry.Timestamp,
            });

        _configSvc.Save(_config);
        Refresh();
    }

    /// <summary>Returns true if the given entry is pinned.</summary>
    public bool IsPinned(ClipboardEntry entry)
    {
        return _config.PinnedClipboard.Any(p => p.Content == entry.Content);
    }

    // ── Internals ───────────────────────────────────────────────────

    private void ApplyFilter()
    {
        var filter = FilterText.Trim();
        var history = _clipboard.GetHistory();

        Entries.Clear();
        foreach (var entry in history)
        {
            if (string.IsNullOrEmpty(filter) ||
                entry.Preview.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                Entries.Add(entry);
            }
        }

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var total = TotalCount;
        var shown = FilteredCount;
        if (string.IsNullOrWhiteSpace(FilterText))
            StatusText = $"{total} item{(total == 1 ? "" : "s")}";
        else
            StatusText = $"{shown} of {total} item{(total == 1 ? "" : "s")}";
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(FilteredCount));
    }
}
