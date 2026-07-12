using System.Text.Json;
using System.Text.Json.Serialization;
using Spur.Helpers;
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
    void KeepOnly(ISet<string> contentToKeep, ISet<Guid>? imageEntryIdsToKeep = null);
    /// <summary>Removes the single entry with the given ID.</summary>
    void RemoveById(Guid id);
    /// <summary>Sets the set of pinned entry contents. Only entries in this set will be persisted to disk.</summary>
    void SetPinnedSet(HashSet<string> pinnedContents);
    /// <summary>Call before CopyToSystem to prevent the watcher from re-adding the entry.</summary>
    void SuppressNextCapture();
    event Action? ClipboardChanged;
}

/// <summary>
/// Lightweight DTO for serializing text clipboard entries to JSON.
/// Images are never persisted.
/// </summary>
internal sealed class ClipboardEntryDto
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// In-memory clipboard history with JSON persistence for text entries.
/// Thread-safe. Deduplicates consecutive identical text entries. Max items
/// is configurable via <see cref="MaxItems"/> (default 10, settable from settings).
/// Text entries are persisted to <c>%LocalAppData%\Spur\clipboard_history.json</c>.
/// </summary>
public sealed class ClipboardServiceImpl : IClipboardService, IDisposable
{
    private int _maxItems = 10;
    private const int MaxImageEntries = 5;
    private readonly List<ClipboardEntry> _history = [];
    private readonly object _lock = new();
    private readonly ILogger _log;

    // ── Persistence ──────────────────────────────────────────────────
    private readonly string _savePath;
    private readonly System.Timers.Timer _debounceTimer;
    private bool _disposed;

    /// <summary>
    /// When true, the next Add/AddImage call from the ClipboardWatcher will be
    /// silently ignored. Reset to false after one suppression. This prevents
    /// duplication when Copy() puts an entry on the system clipboard.
    /// </summary>
    private volatile bool _suppressNext;
    private HashSet<string>? _pinnedContents;

    public event Action? ClipboardChanged;

    public ClipboardServiceImpl(ILogger log)
    {
        _log = log;

        _savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Spur", "clipboard_history.json");

        // Debounce timer: saves at most once every 2 seconds
        _debounceTimer = new System.Timers.Timer(2000) { AutoReset = false };
        _debounceTimer.Elapsed += (_, _) => FlushSave();

        LoadHistory();
    }

    /// <summary>Call this before CopyToSystem to prevent the watcher from re-adding the entry.</summary>
    public void SuppressNextCapture() => _suppressNext = true;

    public void SetPinnedSet(HashSet<string> pinnedContents)
    {
        _pinnedContents = pinnedContents;
    }

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

        // If suppressed (internal copy), skip this capture
        if (_suppressNext)
        {
            _suppressNext = false;
            return;
        }

        var textHash = text.GetHashCode(StringComparison.Ordinal);
        var storedText = text.Length > ClipboardEntry.MaxStoredTextChars
            ? text[..ClipboardEntry.MaxStoredTextChars]
            : text;

        lock (_lock)
        {
            // Dedup: if the same text already exists anywhere, move it to the top
            var existingIdx = _history.FindIndex(e => !e.IsImage && e.FullTextHash == textHash && e.Content == storedText);
            if (existingIdx >= 0)
            {
                // Already at the top — nothing to do
                if (existingIdx == 0) return;
                _history.RemoveAt(existingIdx);
            }

            _history.Insert(0, new ClipboardEntry(storedText));
            EnforceLimits();
        }
        RaiseClipboardChanged();
        ScheduleSave();
    }

    public void AddImage(System.Windows.Media.Imaging.BitmapSource image)
    {
        if (image is null) return;

        // If suppressed (internal copy), skip this capture
        if (_suppressNext)
        {
            _suppressNext = false;
            return;
        }

        if (!image.IsFrozen) image.Freeze();

        // Dedup: compute fingerprint and check if the same image exists already
        var fp = ComputeImageFingerprint(image);
        lock (_lock)
        {
            var existingIdx = _history.FindIndex(e => e.IsImage && e.ImageFingerprint == fp);
            if (existingIdx >= 0)
            {
                if (existingIdx == 0) return; // already at top
                _history.RemoveAt(existingIdx);
            }

            _history.Insert(0, new ClipboardEntry(image) { ImageFingerprint = fp });
            EnforceLimits();
        }
        RaiseClipboardChanged();
    }

    /// <summary>Fast image fingerprint: dimensions + first row sample.</summary>
    private static int ComputeImageFingerprint(System.Windows.Media.Imaging.BitmapSource img)
        => ImageFingerprintHelper.Compute(img);

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
    /// Copies the entry to the system clipboard. Internally uses <c>System.Windows.Clipboard</c>
    /// which requires STA. If called from MTA, marshals to the WPF dispatcher thread.
    /// </summary>
    public void CopyToSystem(ClipboardEntry entry)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => CopyToSystem(entry));
            return;
        }
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
        ScheduleSave();
    }

    /// <summary>
    /// Retains only text entries whose content appears in <paramref name="contentToKeep"/>.
    /// Image entries are always removed because they cannot be pinned.
    /// </summary>
    public void KeepOnly(ISet<string> contentToKeep, ISet<Guid>? imageEntryIdsToKeep = null)
    {
        bool changed = false;
        lock (_lock)
        {
            int removed = _history.RemoveAll(e =>
            {
                if (e.IsImage)
                    return imageEntryIdsToKeep is null || !imageEntryIdsToKeep.Contains(e.Id);
                return !contentToKeep.Contains(e.Content ?? string.Empty);
            });
            changed = removed > 0;
        }
        if (changed)
        {
            RaiseClipboardChanged();
            ScheduleSave();
        }
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
        if (changed)
        {
            RaiseClipboardChanged();
            ScheduleSave();
        }
    }

    // Captures the delegate before null-checking to eliminate the race between
    // the null check and the invocation when a subscriber unregisters concurrently.
    private void RaiseClipboardChanged()
    {
        var handler = ClipboardChanged;
        handler?.Invoke();
    }

    // ── Persistence helpers ─────────────────────────────────────────

    private void ScheduleSave()
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void FlushSave()
    {
        try
        {
            List<ClipboardEntryDto> dtos;
            lock (_lock)
            {
                var pinned = _pinnedContents;
                dtos = _history
                    .Where(e => !e.IsImage && !string.IsNullOrEmpty(e.Content))
                    .Where(e => pinned is null || pinned.Contains(e.Content ?? string.Empty))
                    .Select(e => new ClipboardEntryDto { Content = e.Content, Timestamp = e.Timestamp })
                    .ToList();
            }
            var dir = Path.GetDirectoryName(_savePath)!;
            Directory.CreateDirectory(dir);
            var json = System.Text.Json.JsonSerializer.Serialize(dtos);
            File.WriteAllText(_savePath, json);
        }
        catch (Exception ex)
        {
            _log.Warning("Failed to save clipboard history", ex);
        }
    }

    private void LoadHistory()
    {
        try
        {
            if (!File.Exists(_savePath)) return;
            var json = File.ReadAllText(_savePath);
            var dtos = System.Text.Json.JsonSerializer.Deserialize<List<ClipboardEntryDto>>(json);
            if (dtos is null) return;
            lock (_lock)
            {
                foreach (var dto in dtos)
                {
                    if (!string.IsNullOrEmpty(dto.Content))
                        _history.Add(new ClipboardEntry(dto.Content, dto.Timestamp));
                }
            }
        }
        catch (Exception ex)
        {
            _log.Warning("Failed to load clipboard history", ex);
        }
    }

    /// <summary>Flushes clipboard history to disk.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _debounceTimer.Stop();
        _debounceTimer.Dispose();
        FlushSave();
    }
}
