namespace Volt.Services;

/// <summary>
/// Persists per-path launch frequency counts to disk.
/// Stored as a JSON dictionary at %LocalAppData%\Volt\volt.freq.json.
/// </summary>
public sealed class FrequencyService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly string _path;
    private readonly Dictionary<string, int> _counts = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public FrequencyService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Volt");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "volt.freq.json");
        Load();
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
            Save();
        }
    }

    public void ClearAll()
    {
        lock (_lock) { _counts.Clear(); Save(); }
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
        catch { /* corrupt → start fresh */ }
    }

    private void Save()
    {
        try { File.WriteAllText(_path, JsonSerializer.Serialize(_counts, JsonOpts)); }
        catch (Exception ex) { Debug.WriteLine($"[Volt] Freq save failed: {ex.Message}"); }
    }
}
