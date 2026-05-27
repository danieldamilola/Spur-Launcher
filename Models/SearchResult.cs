namespace Volt.Models;

/// <summary>Result type discriminator.</summary>
public enum ResultType { App, File, Clipboard, Action }

/// <summary>
/// Unified search result for apps, files, clipboard items, and built-in actions.
/// </summary>
public class SearchResult
{
    public string Id { get; set; } = string.Empty;
    public ResultType Type { get; set; }

    /// <summary>Primary display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Secondary line — path, description, or preview.</summary>
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>Path used by PathToIconConverter. May be null for non-file results.</summary>
    public string? IconPath { get; set; }

    /// <summary>Lucide icon name used for non-file results (e.g. "clipboard", "zap").</summary>
    public string? LucideIcon { get; set; }

    public double Score { get; set; }
    public double FrequencyScore { get; set; }

    /// <summary>Whether this item is pinned by the user.</summary>
    public bool IsPinned { get; set; }

    // ── App-specific ──────────────────────────────────
    public string? ExePath { get; set; }
    public string? LnkPath { get; set; }

    // ── File-specific ─────────────────────────────────
    public string? FilePath { get; set; }
    public string? FileExtension { get; set; }

    // ── Clipboard-specific ────────────────────────────
    public string? ClipContent { get; set; }
    public DateTime ClipTimestamp { get; set; }

    // ── Action-specific ───────────────────────────────
    public string? ActionId { get; set; }
}

/// <summary>Section header shown between groups of results (e.g. "APPLICATIONS", "FILES").</summary>
public record SectionLabel(string Title);
