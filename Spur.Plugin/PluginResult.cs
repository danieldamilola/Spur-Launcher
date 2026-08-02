namespace Spur.Plugin;

public class PluginResult
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public int Score { get; set; }

    public Func<PluginActionContext, Task<bool>>? Action { get; set; }

    /// <summary>
    /// Optional reference to the full source object (e.g., SearchResult).
    /// When set, consumers should use this instead of re-building from individual fields.
    /// Null for externally-loaded plugins.
    /// </summary>
    public object? Source { get; set; }
}

