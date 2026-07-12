namespace Spur.Models;

/// <summary>Persisted pinned clipboard entry.</summary>
public sealed record PinnedClipboardItem
{
    /// <summary>Stable identifier (e.g., hashed content).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Full clipboard text content (empty for images).</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>User-facing preview used in lists.</summary>
    public string Preview { get; init; } = string.Empty;

    /// <summary>Original timestamp when pinned.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Entry GUID (used for image pinning where Content is empty).</summary>
    public Guid EntryId { get; init; } = Guid.Empty;
}
