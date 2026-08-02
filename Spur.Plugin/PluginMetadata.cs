namespace Spur.Plugin;

public class PluginMetadata
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string PluginDirectory { get; init; } = string.Empty;

    /// <summary>Delay in ms before this plugin's first query after a keystroke.</summary>
    public int SearchDelayTime { get; init; }
}
