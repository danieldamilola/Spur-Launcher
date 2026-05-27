namespace Volt.Services;

/// <summary>
/// In-memory clipboard text history. Thread-safe. Never persisted.
/// Deduplicates consecutive identical entries and caps at 20 items.
/// </summary>
public static class ClipboardService
{
    private const int MaxItems = 20;

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
            while (_history.Count > MaxItems)
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

    public static void Clear()
    {
        lock (_lock) _history.Clear();
    }
}
