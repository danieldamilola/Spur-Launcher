using Spur.Models;

namespace Spur.Services;

/// <summary>Interface for in-memory clipboard history management.</summary>
public interface IClipboardService
{
    int MaxItems { get; set; }
    IReadOnlyList<ClipboardEntry> GetHistory();
    void Add(string text);
    void AddImage(System.Windows.Media.Imaging.BitmapSource image);
    /// <summary>Copies the entry to the system clipboard (text or image).</summary>
    void CopyToSystem(ClipboardEntry entry);
    /// <summary>Copies a plain string to the system clipboard.</summary>
    void CopyTextToSystem(string text);
    string? ReadFromSystem();
    System.Windows.Media.Imaging.BitmapSource? ReadImageFromSystem();
    void Clear();
    /// <summary>Removes all entries whose text content is not in the keep set. Images are always removed.</summary>
    void KeepOnly(ISet<string> contentToKeep);
    /// <summary>Removes the single entry with the given ID.</summary>
    void RemoveById(Guid id);
    event Action? ClipboardChanged;
}

/// <summary>
/// In-memory clipboard history. Thread-safe. Never persisted.
/// Deduplicates consecutive identical text entries. Max items is configurable
/// via <see cref="MaxItems"/> (default 10, settable from settings).
/// </summary>
public sealed class ClipboardServiceImpl : IClipboardService
{
    private int _maxItems = 10;
    private const int MaxImageEntries = 5;
    private readonly List<ClipboardEntry> _history = [];
    private readonly object _lock = new();
    private readonly ILogger _log;

    public event Action? ClipboardChanged;

    public ClipboardServiceImpl(ILogger log) => _log = log;

    public int MaxItems
    {
        get => _maxItems;
        set
        {
            _maxItems = Math.Clamp(value, 5, 200);
            bool changed = false;
            lock (_lock)
            {
                while (_history.Count > _maxItems)
                {
                    _history.RemoveAt(_history.Count - 1);
                    changed = true;
                }
            }
            if (changed) RaiseClipboardChanged();
        }
    }

    public IReadOnlyList<ClipboardEntry> GetHistory()
    {
        lock (_lock) return _history.ToArray();
    }

    public void Add(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var textHash = text.GetHashCode(StringComparison.Ordinal);

        lock (_lock)
        {
            // Dedup using FullTextHash (computed from the original untruncated text).
            // This correctly handles two long texts whose first 20k chars match but
            // differ beyond the truncation point — they will have different hashes.
            // Collisions are astronomically rare for different texts and the worst
            // case is a missed history entry, never data loss.
            if (_history.Count > 0 &&
                _history[0] is { IsImage: false, FullTextHash: var h } &&
                h == textHash)
                return;

            var storedText = text.Length > ClipboardEntry.MaxStoredTextChars
                ? text[..ClipboardEntry.MaxStoredTextChars]
                : text;

            _history.Insert(0, new ClipboardEntry(storedText));
            EnforceLimits();
        }
        RaiseClipboardChanged();
    }

    public void AddImage(System.Windows.Media.Imaging.BitmapSource image)
    {
        if (image is null) return;
        if (!image.IsFrozen) image.Freeze();

        lock (_lock)
        {
            _history.Insert(0, new ClipboardEntry(image));
            EnforceLimits();
        }
        RaiseClipboardChanged();
    }

    private void TrimImages()
    {
        var imageCount = 0;
        for (var i = 0; i < _history.Count; i++)
        {
            if (!_history[i].IsImage) continue;
            imageCount++;
            if (imageCount > MaxImageEntries)
            {
                _history.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// Enforces both the per-total-count cap (<see cref="MaxItems"/>) and the
    /// per-image-type cap (<see cref="MaxImageEntries"/>). Called from both
    /// <see cref="Add"/> and <see cref="AddImage"/> to keep limits consistent
    /// regardless of which Add overload is used.
    /// </summary>
    private void EnforceLimits()
    {
        TrimImages();
        while (_history.Count > _maxItems)
            _history.RemoveAt(_history.Count - 1);
    }

    /// <summary>
    /// Copies the entry to the system clipboard. Must be called from an STA thread
    /// (the WPF UI thread). Internally uses <c>System.Windows.Clipboard</c> which
    /// throws if called from MTA.
    /// </summary>
    public void CopyToSystem(ClipboardEntry entry)
    {
        try
        {
            if (entry.IsImage && entry.Image is not null)
                Clipboard.SetImage(entry.Image);
            else if (!string.IsNullOrEmpty(entry.Content))
                Clipboard.SetText(entry.Content);
        }
        catch (Exception ex)
        {
            _log.Warning("Clipboard copy failed", ex);
        }
    }

    /// <inheritdoc />
    public void CopyTextToSystem(string text)
    {
        if (!string.IsNullOrEmpty(text))
            CopyToSystem(new ClipboardEntry(text));
    }

    public string? ReadFromSystem()
    {
        try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
        catch { return null; }
    }

    public System.Windows.Media.Imaging.BitmapSource? ReadImageFromSystem()
    {
        try { return Clipboard.ContainsImage() ? Clipboard.GetImage() : null; }
        catch { return null; }
    }

    public void Clear()
    {
        lock (_lock) _history.Clear();
        RaiseClipboardChanged();
    }

    /// <summary>
    /// Retains only text entries whose content appears in <paramref name="contentToKeep"/>.
    /// Image entries are always removed because they cannot be pinned.
    /// </summary>
    public void KeepOnly(ISet<string> contentToKeep)
    {
        bool changed = false;
        lock (_lock)
        {
            int removed = _history.RemoveAll(e =>
                e.IsImage || !contentToKeep.Contains(e.Content ?? string.Empty));
            changed = removed > 0;
        }
        if (changed) RaiseClipboardChanged();
    }

    /// <summary>
    /// Removes the single entry matching <paramref name="id"/>. Previously the
    /// ViewModel used content-string matching, which deleted every entry sharing
    /// the same content and was a no-op for image entries.
    /// </summary>
    public void RemoveById(Guid id)
    {
        bool changed = false;
        lock (_lock)
        {
            int removed = _history.RemoveAll(e => e.Id == id);
            changed = removed > 0;
        }
        if (changed) RaiseClipboardChanged();
    }

    // Captures the delegate before null-checking to eliminate the race between
    // the null check and the invocation when a subscriber unregisters concurrently.
    private void RaiseClipboardChanged()
    {
        var handler = ClipboardChanged;
        handler?.Invoke();
    }
}
