namespace Arc.Services;

/// <summary>Interface for in-memory clipboard history management.</summary>
public interface IClipboardService
{
    int MaxItems { get; set; }
    IReadOnlyList<ClipboardEntry> GetHistory();
    void Add(string text);
    void AddImage(System.Windows.Media.Imaging.BitmapSource image);
    void CopyToSystem(string text);
    string? ReadFromSystem();
    System.Windows.Media.Imaging.BitmapSource? ReadImageFromSystem();
    void Clear();
}

/// <summary>
/// In-memory clipboard text history. Thread-safe. Never persisted.
/// Deduplicates consecutive identical entries.  Max items is configurable
/// via <see cref="MaxItems"/> (default 20, settable from settings).
/// </summary>
public sealed class ClipboardServiceImpl : IClipboardService
{
    private int _maxItems = 10;
    private const int MaxImageEntries = 5;
    private readonly List<ClipboardEntry> _history = [];
    private readonly object _lock = new();
    private readonly ILogger _log;

    public ClipboardServiceImpl(ILogger log) => _log = log;

    public int MaxItems
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

    public IReadOnlyList<ClipboardEntry> GetHistory()
    {
        lock (_lock) return _history.ToArray();
    }

    public void Add(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        lock (_lock)
        {
            var storedText = text.Length > ClipboardEntry.MaxStoredTextChars
                ? text[..ClipboardEntry.MaxStoredTextChars]
                : text;

            if (_history.Count > 0 &&
                string.Equals(_history[0].Content, storedText, StringComparison.Ordinal))
                return;

            _history.Insert(0, new ClipboardEntry(text));
            while (_history.Count > _maxItems)
                _history.RemoveAt(_history.Count - 1);
        }
    }

    public void AddImage(System.Windows.Media.Imaging.BitmapSource image)
    {
        if (image is null) return;
        if (!image.IsFrozen) image.Freeze();

        lock (_lock)
        {
            _history.Insert(0, new ClipboardEntry(image));
            TrimImages();
            while (_history.Count > _maxItems)
                _history.RemoveAt(_history.Count - 1);
        }
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

    public void CopyToSystem(string text)
    {
        try { Clipboard.SetText(text); }
        catch (Exception ex) { _log.Warning("Clipboard copy failed", ex); }
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
    }
}

/// <summary>Static facade — delegates to the configured IClipboardService instance.</summary>
public static class ClipboardService
{
    private static IClipboardService? _instance;
    private static ILogger _log = NullLogger.Instance;

    /// <summary>Sets the underlying instance and logger. Call once at startup.</summary>
    public static void Initialize(IClipboardService instance, ILogger logger)
    {
        _instance = instance;
        _log = logger;
    }

    public static int MaxItems { get => Instance.MaxItems; set => Instance.MaxItems = value; }
    public static IReadOnlyList<ClipboardEntry> GetHistory() => Instance.GetHistory();
    public static void Add(string text) => Instance.Add(text);
    public static void AddImage(System.Windows.Media.Imaging.BitmapSource image) => Instance.AddImage(image);
    public static void CopyToSystem(string text) => Instance.CopyToSystem(text);
    public static string? ReadFromSystem() => Instance.ReadFromSystem();
    public static System.Windows.Media.Imaging.BitmapSource? ReadImageFromSystem() => Instance.ReadImageFromSystem();
    public static void Clear() => Instance.Clear();

    private static IClipboardService Instance
    {
        get
        {
            if (_instance is not null) return _instance;
            // Auto-initialize with defaults if never configured (backward compat)
            var impl = new ClipboardServiceImpl(_log);
            _instance = impl;
            _log.Info("ClipboardService auto-initialized with defaults.");
            return impl;
        }
    }
}
