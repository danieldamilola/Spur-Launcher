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
    private System.Windows.Threading.DispatcherTimer? _filterDebounce;

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

    partial void OnFilterTextChanged(string value)
    {
        if (_filterDebounce is null)
        {
            _filterDebounce = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _filterDebounce.Tick += (_, _) =>
            {
                _filterDebounce.Stop();
                Refresh();
            };
        }
        _filterDebounce.Stop();
        _filterDebounce.Start();
    }

    [ObservableProperty]
    private ClipboardEntry? _selectedEntry;

    partial void OnSelectedEntryChanged(ClipboardEntry? value)
    {
        OnPropertyChanged(nameof(SelectedContent));
        OnPropertyChanged(nameof(SelectedMetadata));
        IsImageSelected = value?.IsImage ?? false;
    }

    /// <summary>Full text of the selected entry (shown in detail area).</summary>
    public string? SelectedContent => SelectedEntry?.Content;

    /// <summary>Metadata string for the detail panel (type, char/word/line counts, or image dims).</summary>
    public string? SelectedMetadata
    {
        get
        {
            var entry = SelectedEntry;
            if (entry is null) return null;

            if (entry.IsImage)
            {
                var img = entry.Image;
                return img is not null ? $"Image · {img.PixelWidth} × {img.PixelHeight}" : "Image";
            }

            var content = entry.Content ?? "";
            var chars = content.Length;
            var words = content.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries).Length;
            var lines = content.Split('\n').Length;
            var type = DetectContentType(content);
            return $"{type} · {chars:N0} chars · {words:N0} words · {lines:N0} lines";
        }
    }

    public bool HasPinnedEntries => Entries.Any(e => e.IsPinned);

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

        // Stamp pin status BEFORE sorting so pinned items float to the top.
        var pinnedSet = _config.PinnedClipboard
            .Select(p => p.Content)
            .Where(c => !string.IsNullOrEmpty(c))
            .ToHashSet(StringComparer.Ordinal);
        var pinnedImageIds = _config.PinnedClipboard
            .Where(p => p.EntryId != Guid.Empty)
            .Select(p => p.EntryId)
            .ToHashSet();
        _clipboard.SetPinnedSet(pinnedSet);

        foreach (var entry in desired)
            entry.IsPinned = string.IsNullOrEmpty(entry.Content)
                ? pinnedImageIds.Contains(entry.Id)
                : pinnedSet.Contains(entry.Content);

        desired = desired.OrderByDescending(e => e.IsPinned).ToList();

        SyncEntries(desired);

        // Always select the first item so the selection rail starts at the top
        if (Entries.Count > 0)
            SelectedEntry = Entries[0];
        else
            SelectedEntry = null;

        OnPropertyChanged(nameof(HasPinnedEntries));
        UpdateStatus();
    }

    /// <summary>Copy a single entry to the system clipboard.</summary>
    [RelayCommand]
    public void Copy(ClipboardEntry? entry)
    {
        if (entry is null) return;

        _lastCopyTimestamp = DateTime.UtcNow;
        _clipboard.SuppressNextCapture();

        // Try clipboard write first — if it fails, don't reorder history
        try
        {
            _clipboard.CopyToSystem(entry);
        }
        catch
        {
            StatusText = "Copy failed ✗";
            return;
        }

        // Success — promote to top of history
        _clipboard.RemoveById(entry.Id);
        if (entry.IsImage)
        {
            if (entry.Image is not null)
                _clipboard.AddImage(entry.Image);
        }
        else
        {
            if (entry.Content is not null)
                _clipboard.Add(entry.Content);
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
        var pinnedImageIds = _config.PinnedClipboard
            .Where(p => p.EntryId != Guid.Empty)
            .Select(p => p.EntryId)
            .ToHashSet();
        _clipboard.KeepOnly(pinned, pinnedImageIds);
        Refresh();
    }

    /// <summary>Toggle pin status for an entry.</summary>
    [RelayCommand]
    public void TogglePin(ClipboardEntry? entry)
    {
        if (entry is null) return;

        // Match by Content for text entries, EntryId for images
        var existing = string.IsNullOrEmpty(entry.Content)
            ? _config.PinnedClipboard.FirstOrDefault(p => p.EntryId == entry.Id)
            : _config.PinnedClipboard.FirstOrDefault(p => p.Content == entry.Content);

        if (existing is not null)
            _config.PinnedClipboard.Remove(existing);
        else
            _config.PinnedClipboard.Add(new PinnedClipboardItem
            {
                Id        = $"clip:{entry.Timestamp.Ticks}",
                Content   = entry.Content ?? string.Empty,
                Preview   = entry.Preview,
                Timestamp = entry.Timestamp,
                EntryId   = entry.Id,
            });

        _configSvc.Save(_config);
        OnPropertyChanged(nameof(HasPinnedEntries));
        Refresh();
    }

    /// <summary>Returns true if the given entry is pinned.</summary>
    public bool IsPinned(ClipboardEntry entry)
    {
        return string.IsNullOrEmpty(entry.Content)
            ? _config.PinnedClipboard.Any(p => p.EntryId == entry.Id)
            : _config.PinnedClipboard.Any(p => p.Content == entry.Content);
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

    /// <summary>Forces re-evaluation of TimeAgo bindings by triggering a collection replace notification.</summary>
    public void RefreshTimestamps()
    {
        for (int i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            Entries[i] = entry; // Replace-in-place triggers binding refresh
        }
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

    private static string DetectContentType(string content)
    {
        var trimmed = content.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
            return "\U0001F517 URL";
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try { System.Text.Json.JsonDocument.Parse(trimmed); return "\U0001F4CB JSON"; } catch { /* not valid JSON */ }
        }
        if (trimmed.Contains('@') && trimmed.Contains('.') && !trimmed.Contains(' ') && trimmed.Length < 320)
            return "\U0001F4E7 Email";

        // Require 2+ signals to reduce false positives
        int codeSignals = 0;
        ReadOnlySpan<string> codeKeywords = ["function ", "class ", "def ", "const ", "var ", "public ", "private ", "import ", "#include", "using ", "return ", "if (", "for (", "while ("];
        foreach (var kw in codeKeywords)
            if (trimmed.Contains(kw)) codeSignals++;
        if (codeSignals >= 2)
            return "\U0001F4BB Code";

        if (System.IO.Path.IsPathFullyQualified(trimmed) || trimmed.StartsWith("C:\\") || trimmed.StartsWith("/"))
            return "\U0001F4C1 Path";
        return "\U0001F4DD Text";
    }

    /// <summary>Merge the selected entry with the entry directly above it using the given separator.</summary>
    [RelayCommand]
    public void MergeWith(string? separator)
    {
        if (SelectedEntry is null || SelectedEntry.IsImage) return;
        var idx = Entries.IndexOf(SelectedEntry);
        if (idx <= 0) return;
        var above = Entries[idx - 1];
        if (above.IsImage) return;

        var sep = separator switch
        {
            "space"   => " ",
            "newline" => Environment.NewLine,
            _         => ""   // "none" or default — direct concatenation
        };

        var merged = above.Content + sep + SelectedEntry.Content;

        _clipboard.RemoveById(above.Id);
        _clipboard.RemoveById(SelectedEntry.Id);

        _clipboard.SuppressNextCapture();
        _clipboard.Add(merged);

        StatusText = "Merged ✓";
        _lastCopyTimestamp = DateTime.UtcNow;
        Refresh();
    }

    // ── Cleanup ─────────────────────────────────────────────────────

    public void Dispose()
    {
        _clipboard.ClipboardChanged -= _clipboardChangedHandler;
        _filterDebounce?.Stop();
    }
}
