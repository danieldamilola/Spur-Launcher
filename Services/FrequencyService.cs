namespace Arc.Services;

/// <summary>
/// Persists per-path launch frequency counts to disk with debounced writes.
/// Stored as a JSON dictionary at %LocalAppData%\Arc\Arc.freq.json.
/// Writes are batched — at most one disk write per 30 seconds.
/// </summary>
public sealed class FrequencyService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly string _path;
    private readonly Dictionary<string, int> _counts = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private readonly ILogger _log;
    private readonly System.Timers.Timer _debounce;
    private bool _dirty;
    private bool _disposed;

    public FrequencyService(ILogger? log = null)
    {
        _log = log ?? NullLogger.Instance;

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Arc");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "Arc.freq.json");
        Load();

        // Debounce writes: save at most once per 30 seconds
        _debounce = new System.Timers.Timer(30_000) { AutoReset = false };
        _debounce.Elapsed += (_, _) => Flush();
    }

    public int Get(string path)
    {
        if (string.IsNullOrEmpty(path)) return 0;
        lock (_lock)
            return _counts.TryGetValue(path, out var v) ? v : 0;
    }

    public void Increment(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        lock (_lock)
        {
            _counts.TryGetValue(path, out var cur);
            _counts[path] = cur + 1;
            _dirty = true;
            _debounce.Stop();
            _debounce.Start();
        }
    }

    public void ClearAll()
    {
        lock (_lock) { _counts.Clear(); _dirty = true; }
        Flush();
    }

    /// <summary>Forces an immediate write. Called by the timer and on shutdown.</summary>
    public void Flush()
    {
        lock (_lock)
        {
            if (!_dirty) return;
            Save();
            _dirty = false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _debounce.Stop();
        _debounce.Dispose();
        Flush();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;
            var data = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(_path));
            if (data is null) return;
            lock (_lock)
                foreach (var kv in data) _counts[kv.Key] = kv.Value;
        }
        catch (Exception ex) { _log.Warning("Frequency load failed — starting fresh", ex); }
    }

    private void Save()
    {
        try { File.WriteAllText(_path, JsonSerializer.Serialize(_counts, JsonOpts)); }
        catch (Exception ex) { _log.Warning("Frequency save failed", ex); }
    }
}
