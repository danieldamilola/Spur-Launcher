namespace Volt.Services;

/// <summary>
/// Reads and writes <see cref="VoltConfig"/> as JSON to
/// <c>%LocalAppData%\Volt\volt.config.json</c>.
/// </summary>
public sealed class ConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;

    public ConfigService()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Volt");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "volt.config.json");
    }

    public VoltConfig Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<VoltConfig>(json) ?? new VoltConfig();
            }
        }
        catch { /* corrupt file — return defaults */ }

        var defaults = new VoltConfig();
        Save(defaults);
        return defaults;
    }

    public void Save(VoltConfig config)
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(config, JsonOptions));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Volt] Config save failed: {ex.Message}");
        }
    }

    public Task<VoltConfig> LoadAsync() => Task.Run(Load);
    public Task SaveAsync(VoltConfig config) => Task.Run(() => Save(config));
}
