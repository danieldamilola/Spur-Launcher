namespace Arc.Services;

/// <summary>Interface for config persistence.</summary>
public interface IConfigService
{
    ArcConfig Load();
    void Save(ArcConfig config);
    Task<ArcConfig> LoadAsync();
    Task SaveAsync(ArcConfig config);
}

/// <summary>
/// Reads and writes <see cref="ArcConfig"/> as JSON to
/// <c>%LocalAppData%\Arc\Arc.config.json</c>.
/// </summary>
public sealed class ConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private static readonly SemaphoreSlim SaveGate = new(1, 1);

    private readonly string _path;
    private readonly ILogger _log;

    public ConfigService(ILogger? log = null)
    {
        _log = log ?? NullLogger.Instance;

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Arc");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "arc.config.json");
    }

    public ArcConfig Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<ArcConfig>(json) ?? new ArcConfig();
            }
        }
        catch (Exception ex) { _log.Warning("Config load failed — using defaults", ex); }

        var defaults = new ArcConfig();
        Save(defaults);
        return defaults;
    }

    /// <summary>
    /// Synchronously persists <paramref name="config"/> to disk.
    /// Uses the same semaphore as <see cref="SaveAsync"/> to prevent concurrent writes.
    /// </summary>
    public void Save(ArcConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        SaveGate.Wait();
        try { File.WriteAllText(_path, json); }
        catch (Exception ex) { _log.Warning("Config save failed", ex); }
        finally { SaveGate.Release(); }
    }

    public Task<ArcConfig> LoadAsync() => Task.Run(Load);
    public async Task SaveAsync(ArcConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await SaveGate.WaitAsync();
        try { await File.WriteAllTextAsync(_path, json); }
        catch (Exception ex) { _log.Warning("Config save failed", ex); }
        finally { SaveGate.Release(); }
    }
}
