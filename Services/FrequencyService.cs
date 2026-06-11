namespace Spur.Services;

public interface IFrequencyService : IDisposable
{
    int Get(string path);
    void Increment(string path);
    void ClearAll();
    void Flush();
}

/// <summary>
/// Persists per-path launch frequency counts to disk with debounced writes.
/// Stored as a JSON dictionary at %LocalAppData%\Spur\Spur.freq.json.
/// Writes are batched — at most one disk write per 30 seconds.
/// </summary>
public sealed class FrequencyService : IFrequencyService
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
            "Spur");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "Spur.freq.json");
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
        Dictionary<string, int> snapshot;
        lock (_lock)
        {
            if (!_dirty) return;
            // Snapshot the data under the lock, then write outside it.
            // This avoids holding the lock during synchronous disk I/O,
            // which could block Increment() calls from the UI thread.
            snapshot = new Dictionary<string, int>(_counts, _counts.Comparer);
            _dirty = false;
        }

        SaveToDisk(snapshot);
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
        catch (Exception ex)
        {
            _log.Warning("Frequency load failed — starting fresh", ex);
            // Delete the corrupt file so the next startup doesn't re-trigger the same error.
            try { File.Delete(_path); } catch { /* best effort */ }
        }
    }

    /// <summary>Writes the snapshot to disk. Called outside the lock.</summary>
    private void SaveToDisk(Dictionary<string, int> data)
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(data, JsonOpts));
        }
        catch (Exception ex)
        {
            _log.Warning("Frequency save failed", ex);
            // Re-mark as dirty so the next timer tick retries the write.
            lock (_lock) { _dirty = true; }
        }
    }
}
