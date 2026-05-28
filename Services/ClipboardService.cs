namespace Volt.Services;

/// <summary>
/// In-memory clipboard text history. Thread-safe. Never persisted.
/// Deduplicates consecutive identical entries.  Max items is configurable
/// via <see cref="MaxItems"/> (default 20, settable from settings).
/// </summary>
public static class ClipboardService
{
    private static int _maxItems = 10;

    /// <summary>Maximum number of items kept in history. Clamped to 5–200.</summary>
    public static int MaxItems
    {
        get => _maxItems;
        set
        {
            _maxItems = Math.Clamp(value, 5, 200);
            lock (_lock)
            {
                while (_history.Count > _maxItems)
                    _history.RemoveAt(_history.Count - 1);
            }
        }
    }

    private static readonly List<ClipboardEntry> _history = [];
    private static readonly object _lock = new();

    public static IReadOnlyList<ClipboardEntry> GetHistory()
    {
        lock (_lock) return _history.ToArray();
    }

    /// <summary>Adds a new text entry to the top, deduplicating consecutive identical items.</summary>
    public static void Add(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        lock (_lock)
        {
            if (_history.Count > 0 &&
                string.Equals(_history[0].Content, text, StringComparison.Ordinal))
                return;

            _history.Insert(0, new ClipboardEntry(text));
            while (_history.Count > _maxItems)
                _history.RemoveAt(_history.Count - 1);
        }
    }

    /// <summary>Adds a clipboard image entry. The BitmapSource is frozen for cross-thread safety.</summary>
    public static void AddImage(System.Windows.Media.Imaging.BitmapSource image)
    {
        if (image is null) return;
        if (!image.IsFrozen) image.Freeze();

        lock (_lock)
        {
            _history.Insert(0, new ClipboardEntry(image));
            while (_history.Count > _maxItems)
                _history.RemoveAt(_history.Count - 1);
        }
    }

    /// <summary>Writes text to the system clipboard. Must be called on the UI thread.</summary>
    public static void CopyToSystem(string text)
    {
        try { Clipboard.SetText(text); }
        catch (Exception ex) { Debug.WriteLine($"[Volt] Clipboard copy failed: {ex.Message}"); }
    }

    /// <summary>Reads current text from system clipboard. Returns null if no text.</summary>
    public static string? ReadFromSystem()
    {
        try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
        catch { return null; }
    }

    /// <summary>Reads an image from the system clipboard. Must be called on UI thread.</summary>
    public static System.Windows.Media.Imaging.BitmapSource? ReadImageFromSystem()
    {
        try { return Clipboard.ContainsImage() ? Clipboard.GetImage() : null; }
        catch { return null; }
    }

    public static void Clear()
    {
        lock (_lock) _history.Clear();
    }
}
