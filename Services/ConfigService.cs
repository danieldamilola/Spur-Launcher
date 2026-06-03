namespace Spur.Services;

/// <summary>Interface for config persistence.</summary>
public interface IConfigService
{
    SpurConfig Load();
    void Save(SpurConfig config);
    Task<SpurConfig> LoadAsync();
    Task SaveAsync(SpurConfig config);
}

/// <summary>
/// Reads and writes <see cref="SpurConfig"/> as JSON to
/// <c>%LocalAppData%\Spur\Spur.config.json</c>.
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
            "Spur");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "Spur.config.json");
    }

    public SpurConfig Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<SpurConfig>(json) ?? new SpurConfig();
            }
        }
        catch (Exception ex) { _log.Warning("Config load failed — using defaults", ex); }

        var defaults = new SpurConfig();
        Save(defaults);
        return defaults;
    }

    /// <summary>
    /// Synchronously persists <paramref name="config"/> to disk.
    /// Uses the same semaphore as <see cref="SaveAsync"/> to prevent concurrent writes.
    /// </summary>
    public void Save(SpurConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        SaveGate.Wait();
        try { File.WriteAllText(_path, json); }
        catch (Exception ex) { _log.Warning("Config save failed", ex); }
        finally { SaveGate.Release(); }
    }

    public Task<SpurConfig> LoadAsync() => Task.Run(Load);
    public async Task SaveAsync(SpurConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await SaveGate.WaitAsync();
        try { await File.WriteAllTextAsync(_path, json); }
        catch (Exception ex) { _log.Warning("Config save failed", ex); }
        finally { SaveGate.Release(); }
    }
}

