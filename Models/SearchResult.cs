namespace Arc.Models;

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

    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplaySubtitle => Type switch
    {
        ResultType.App => string.IsNullOrWhiteSpace(LnkPath) ? "Application" : "Application shortcut",
        ResultType.File when IsDirectory => "Folder",
        ResultType.File => string.IsNullOrWhiteSpace(FileExtension) ? "File" : FileExtension.TrimStart('.').ToUpperInvariant(),
        _ => Subtitle,
    };

    [System.Text.Json.Serialization.JsonIgnore]
    public string DetailText => Type switch
    {
        ResultType.App => ExePath ?? LnkPath ?? Subtitle,
        ResultType.File => FilePath ?? Subtitle,
        ResultType.Clipboard => ClipContent ?? Subtitle,
        _ => Subtitle,
    };

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

    /// <summary>True when this is a directory result (for folder-first ranking).</summary>
    public bool IsDirectory { get; set; }

    // ── Clipboard-specific ────────────────────────────
    public string? ClipContent { get; set; }
    public DateTime ClipTimestamp { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public System.Windows.Media.Imaging.BitmapSource? ClipImage { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsClipImage => ClipImage is not null;

    // ── Action-specific ───────────────────────────────
    public string? ActionId { get; set; }
}

/// <summary>Section header shown between groups of results (e.g. "APPLICATIONS", "FILES").</summary>
public record SectionLabel(string Title);

/// <summary>
/// JSON-safe subset of SearchResult — no WPF types, no computed properties.
/// Used as the serialization boundary for cache files.
/// </summary>
public record PersistedSearchResult(
    string Id,
    ResultType Type,
    string Name,
    string Subtitle,
    string? IconPath,
    string? LucideIcon,
    double Score,
    double FrequencyScore,
    bool IsPinned,
    string? ExePath,
    string? LnkPath,
    string? FilePath,
    string? FileExtension,
    bool IsDirectory,
    string? ClipContent,
    DateTime ClipTimestamp,
    string? ActionId
)
{
    public SearchResult ToResult() => new()
    {
        Id = Id, Type = Type, Name = Name, Subtitle = Subtitle,
        IconPath = IconPath, LucideIcon = LucideIcon,
        Score = Score, FrequencyScore = FrequencyScore,
        IsPinned = IsPinned,
        ExePath = ExePath, LnkPath = LnkPath,
        FilePath = FilePath, FileExtension = FileExtension,
        IsDirectory = IsDirectory,
        ClipContent = ClipContent, ClipTimestamp = ClipTimestamp,
        ActionId = ActionId,
    };

    public static PersistedSearchResult FromResult(SearchResult r) => new(
        r.Id, r.Type, r.Name, r.Subtitle,
        r.IconPath, r.LucideIcon,
        r.Score, r.FrequencyScore,
        r.IsPinned,
        r.ExePath, r.LnkPath,
        r.FilePath, r.FileExtension,
        r.IsDirectory,
        r.ClipContent, r.ClipTimestamp,
        r.ActionId
    );
}
