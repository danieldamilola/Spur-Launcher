using Spur.Models;
using Spur.Services;
using Spur.Extensions;

namespace Spur.Plugins;

internal static class PluginHelper
{
    public static double MatchScore(string query, string target, SpurConfig config, SearchResult? result = null)
    {
        if (string.IsNullOrEmpty(query)) return 0;
        if (!config.FuzzySearch)
            return target.Contains(query, StringComparison.OrdinalIgnoreCase) ? 1 : -1;

        var match = FuzzySearch.Match(query, target);
        if (result is not null && match.Success)
            result.TitleHighlightData = match.MatchedIndices as HashSet<int>;
        return match.Score >= MinMatchScore(config) ? match.Score : -1;
    }

    private static double MinMatchScore(SpurConfig config) => config.QuerySearchPrecision switch
    {
        "low" => 0,
        "strict" => 1.2,
        _ => 0.35,
    };

    public static SearchResult Clone(SearchResult s) => new()
    {
        Id = s.Id, Name = s.Name, Subtitle = s.Subtitle, Type = s.Type,
        ExePath = s.ExePath, LnkPath = s.LnkPath, IconPath = s.IconPath, IconGlyph = s.IconGlyph,
        FilePath = s.FilePath, FileExtension = s.FileExtension, IsDirectory = s.IsDirectory,
        ClipContent = s.ClipContent, ClipTimestamp = s.ClipTimestamp, ClipImage = s.ClipImage,
        ActionId = s.ActionId, FrequencyScore = s.FrequencyScore, IsPinned = s.IsPinned,
    };

    public static Spur.Plugin.PluginResult ToPluginResult(SearchResult sr, string section) => new()
    {
        Id = sr.Id,
        Title = sr.Name,
        Subtitle = sr.Subtitle,
        Section = section,
        IconGlyph = sr.IconGlyph,
        IconPath = sr.IconPath ?? string.Empty,
        Score = (int)sr.Score,
        Source = sr,
    };

    public static bool LooksLikeUrl(string query)
        => Uri.TryCreate(NormalizeUrl(query), UriKind.Absolute, out var uri)
           && uri.Scheme is "http" or "https";

    public static string NormalizeUrl(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Contains("://", StringComparison.Ordinal)
            ? trimmed
            : $"https://{trimmed}";
    }
}
