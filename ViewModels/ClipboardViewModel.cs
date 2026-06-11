using System.Collections.ObjectModel;
using Spur.Models;
using Spur.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Spur.ViewModels;

/// <summary>
/// Standalone clipboard manager ViewModel. Extracted from the launcher's main
/// search flow so clipboard browsing, copy, delete, and pinning are self-contained.
/// </summary>
public sealed partial class ClipboardViewModel : ObservableObject, IDisposable
{
    private readonly IClipboardService _clipboard;
    private readonly SpurConfig _config;
    private readonly IConfigService _configSvc;
    private readonly Action _clipboardChangedHandler;

    // Timestamp of the last Copy() call. UpdateStatus() will not overwrite
    // "Copied ✓" for 500ms after this, giving any queued Refresh handlers
    // from ClipboardWatcher time to settle.
    private DateTime _lastCopyTimestamp = DateTime.MinValue;

    public ClipboardViewModel(
        IClipboardService clipboard,
        SpurConfig config,
        IConfigService configSvc)
    {
        _clipboard = clipboard;
        _config    = config;
        _configSvc = configSvc;

        _clipboardChangedHandler = () =>
        {
            if (System.Windows.Application.Current?.Dispatcher.CheckAccess() == true)
                Refresh();
            else
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(Refresh);
        };

        _clipboard.ClipboardChanged += _clipboardChangedHandler;
    }

    // ── Observable properties ──────────────────────────────────────

    public ObservableCollection<ClipboardEntry> Entries { get; } = [];

    [ObservableProperty]
    private string _filterText = string.Empty;

    partial void OnFilterTextChanged(string value) => Refresh();

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

    public int TotalCount    => _clipboard.GetHistory().Count;
    public int FilteredCount => Entries.Count;

    [ObservableProperty]
    private string _statusText = string.Empty;

    // ── Public API ──────────────────────────────────────────────────

    /// <summary>
    /// Refreshes the entry list from the clipboard service using a diff-based
    /// approach to avoid tearing down and rebuilding all ListView containers.
    /// </summary>
    public void Refresh()
    {
        var history = _clipboard.GetHistory();
        var filter  = FilterText.Trim();

        List<ClipboardEntry> desired = string.IsNullOrEmpty(filter)
            ? [.. history]
            : history.Where(e => e.Preview.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        SyncEntries(desired);

        if (Entries.Count > 0 && (SelectedEntry == null || !Entries.Contains(SelectedEntry)))
            SelectedEntry = Entries[0];

        UpdateStatus();
    }

    /// <summary>Copy a single entry to the system clipboard.</summary>
    [RelayCommand]
    public void Copy(ClipboardEntry? entry)
    {
        if (entry is null) return;

        _lastCopyTimestamp = DateTime.UtcNow;

        if (entry.IsImage)
        {
            if (entry.Image is null) return;
            _clipboard.AddImage(entry.Image);
            _clipboard.CopyToSystem(entry);
        }
        else
        {
            if (entry.Content is null) return;
            _clipboard.Add(entry.Content);
            _clipboard.CopyToSystem(entry);
        }

        StatusText = "Copied ✓";
    }

    /// <summary>Remove a single entry from history by its unique ID.</summary>
    [RelayCommand]
    public void Delete(ClipboardEntry? entry)
    {
        if (entry is null) return;
        _clipboard.RemoveById(entry.Id);
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

    /// <summary>Toggle pin status for an entry. Images cannot be pinned.</summary>
    [RelayCommand]
    public void TogglePin(ClipboardEntry? entry)
    {
        if (entry is null || entry.IsImage) return;

        var existing = _config.PinnedClipboard.FirstOrDefault(p => p.Content == entry.Content);
        if (existing is not null)
            _config.PinnedClipboard.Remove(existing);
        else
            _config.PinnedClipboard.Add(new PinnedClipboardItem
            {
                Id        = $"clip:{entry.Timestamp.Ticks}",
                Content   = entry.Content,
                Preview   = entry.Preview,
                Timestamp = entry.Timestamp,
            });

        _configSvc.Save(_config);
        Refresh();
    }

    /// <summary>Returns true if the given entry is pinned. Always false for images.</summary>
    public bool IsPinned(ClipboardEntry entry)
    {
        if (entry.IsImage) return false;
        return _config.PinnedClipboard.Any(p => p.Content == entry.Content);
    }

    // ── Internals ───────────────────────────────────────────────────

    /// <summary>
    /// Diffs Entries against the desired list and applies minimal add/move/remove
    /// operations to avoid the flicker and container rebuild cost of Clear() + re-add.
    /// </summary>
    private void SyncEntries(IReadOnlyList<ClipboardEntry> desired)
    {
        var desiredIds = desired.Select(e => e.Id).ToHashSet();

        for (int i = Entries.Count - 1; i >= 0; i--)
        {
            if (!desiredIds.Contains(Entries[i].Id))
                Entries.RemoveAt(i);
        }

        var existingIndex = new Dictionary<Guid, int>(Entries.Count);
        for (int i = 0; i < Entries.Count; i++)
            existingIndex[Entries[i].Id] = i;

        for (int i = 0; i < desired.Count; i++)
        {
            if (i < Entries.Count && Entries[i].Id == desired[i].Id)
            {
                existingIndex[desired[i].Id] = i;
                continue;
            }

            if (existingIndex.TryGetValue(desired[i].Id, out var existingIdx) && existingIdx >= i)
            {
                var item = Entries[existingIdx];
                Entries.RemoveAt(existingIdx);
                Entries.Insert(i, item);
                existingIndex[item.Id] = i;
            }
            else
            {
                Entries.Insert(i, desired[i]);
                existingIndex[desired[i].Id] = i;
            }
        }

        while (Entries.Count > desired.Count)
            Entries.RemoveAt(Entries.Count - 1);
    }

    public void MoveSelection(int delta)
    {
        if (Entries.Count == 0) return;
        int idx = SelectedEntry is null ? -1 : Entries.IndexOf(SelectedEntry);
        SelectedEntry = Entries[Math.Clamp(idx + delta, 0, Entries.Count - 1)];
    }

    private void UpdateStatus()
    {
        // Preserve "Copied ✓" for 500ms after the last Copy() call,
        // allowing any queued Refresh (e.g. from ClipboardWatcher) to settle.
        if (StatusText == "Copied ✓" &&
            (DateTime.UtcNow - _lastCopyTimestamp).TotalMilliseconds < 500)
            return;

        var total = TotalCount;
        var shown = FilteredCount;
        StatusText = string.IsNullOrWhiteSpace(FilterText)
            ? $"{total} item{(total == 1 ? "" : "s")}"
            : $"{shown} of {total} item{(total == 1 ? "" : "s")}";
    }

    // ── Cleanup ─────────────────────────────────────────────────────

    public void Dispose()
    {
        _clipboard.ClipboardChanged -= _clipboardChangedHandler;
    }
}
