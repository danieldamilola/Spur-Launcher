namespace Arc.Models;

/// <summary>Persisted pinned clipboard entry (text-only).</summary>
public sealed record PinnedClipboardItem
{
    /// <summary>Stable identifier (e.g., hashed content).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Full clipboard text content.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>User-facing preview used in lists.</summary>
    public string Preview { get; init; } = string.Empty;

    /// <summary>Original timestamp when pinned.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
