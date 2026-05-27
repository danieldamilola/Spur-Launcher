namespace Volt.Models;

/// <summary>A single clipboard history entry.</summary>
public sealed class ClipboardEntry
{
    public ClipboardEntry(string content)
    {
        Content   = content;
        Timestamp = DateTime.Now;
        Preview   = content.Length > 60
            ? content[..60].Replace('\n', ' ').Replace('\r', ' ') + "…"
            : content.Replace('\n', ' ').Replace('\r', ' ');
    }

    public string Content   { get; }
    public DateTime Timestamp { get; }

    /// <summary>Truncated single-line preview for display in results list.</summary>
    public string Preview   { get; }

    public string TimeAgo
    {
        get
        {
            var elapsed = DateTime.Now - Timestamp;
            if (elapsed.TotalSeconds < 60) return "just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            return $"{(int)elapsed.TotalHours}h ago";
        }
    }
}
