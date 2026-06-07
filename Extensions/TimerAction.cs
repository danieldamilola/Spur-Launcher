using System.Text.RegularExpressions;

namespace Spur.Extensions;

/// <summary>
/// Timer action. Triggered by "timer [duration]", e.g. "timer 5m", "timer 30s".
/// Sets a countdown timer and shows a notification when it expires.
/// </summary>
public sealed class TimerAction : IAction
{
    public string Id => "timer";
    public string Name => "Timer";
    public string IconGlyph => "\ue121";
    public bool IsGlobal => false;
    public static string PresetText { get; set; } = "1m,3m,5m,10m,15m,30m";

    private static readonly (string label, string input, int seconds)[] _fallbackPresets =
    {
        ("1 minute",            "1m",   60),
        ("3 minutes",           "3m",   180),
        ("5 minutes",           "5m",   300),
        ("10 minutes",          "10m",  600),
        ("15 minutes",          "15m",  900),
        ("30 minutes",          "30m",  1800),
        ("1 hour",              "1h",   3600),
        ("Custom…",             "",     0),
    };

    // ── Keyword-scoped ────────────────────────────────────────────
    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var text = subQuery.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            foreach (var (label, input, _) in GetPresets())
            {
                yield return new SearchResult
                {
                    Id         = $"timer:{input}",
                    Type       = ResultType.Action,
                    Name       = label,
                    Subtitle   = string.IsNullOrEmpty(input) ? "Type a duration like 5m or 30s" : $"timer {input}",
                    IconGlyph  = "\ue121",
                    ActionId   = Id,
                };
            }
            yield break;
        }

        if (TryParseDuration(text, out var totalSeconds))
        {
            var display = FormatDuration(totalSeconds);
            yield return new SearchResult
            {
                Id         = $"timer:{text.ToLowerInvariant()}",
                Type       = ResultType.Action,
                Name       = $"Start Timer — {display}",
                Subtitle   = $"Press ↵ to start a {display.ToLowerInvariant()} countdown",
                IconGlyph  = "\ue121",
                ActionId   = Id,
            };
        }
        else
        {
            yield return new SearchResult
            {
                Id         = $"timer:{text.ToLowerInvariant()}",
                Type       = ResultType.Action,
                Name       = "Invalid duration",
                Subtitle   = "Try something like 5m, 30s, 1h, or 2h30m",
                IconGlyph  = "\ue121",
                ActionId   = Id,
            };
        }
    }

    private static IEnumerable<(string label, string input, int seconds)> GetPresets()
    {
        var yielded = false;
        foreach (var token in PresetText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryParseDuration(token, out var seconds)) continue;
            yielded = true;
            yield return (FormatDuration(seconds), token, seconds);
        }

        if (!yielded)
        {
            foreach (var preset in _fallbackPresets)
                yield return preset;
        }
        else
        {
            yield return ("Custom...", "", 0);
        }
    }

    // ── Legacy (global) ───────────────────────────────────────────
    public bool CanHandle(string query)
        => !string.IsNullOrWhiteSpace(query) && TryParseDuration(query.Trim(), out _);

    public SearchResult BuildResult(string query)
    {
        if (TryParseDuration(query.Trim(), out var totalSeconds))
        {
            var display = FormatDuration(totalSeconds);
            return new SearchResult
            {
                Id         = $"timer:{query.Trim().ToLowerInvariant()}",
                Type       = ResultType.Action,
                Name       = $"Start Timer — {display}",
                Subtitle   = $"Press ↵ to start a {display.ToLowerInvariant()} countdown",
                IconGlyph  = "\ue121",
                ActionId   = Id,
            };
        }
        return new() { Id = "action:timer", Type = ResultType.Action, Name = "Timer", ActionId = Id };
    }

    // ── Duration parsing ──────────────────────────────────────────
    private static bool TryParseDuration(string input, out int totalSeconds)
    {
        totalSeconds = 0;
        var match = Regex.Match(input.Trim(), @"^(?:(\d+)\s*h\s*)?(?:(\d+)\s*m\s*)?(?:(\d+)\s*s)?$", RegexOptions.IgnoreCase);
        if (!match.Success || (match.Groups[1].Value == "" && match.Groups[2].Value == "" && match.Groups[3].Value == ""))
            return false;

        if (int.TryParse(match.Groups[1].Value, out var h)) totalSeconds += h * 3600;
        if (int.TryParse(match.Groups[2].Value, out var m)) totalSeconds += m * 60;
        if (int.TryParse(match.Groups[3].Value, out var s)) totalSeconds += s;

        return totalSeconds > 0;
    }

    private static string FormatDuration(int totalSeconds)
    {
        var h = totalSeconds / 3600;
        var m = (totalSeconds % 3600) / 60;
        var s = totalSeconds % 60;

        if (h > 0 && m > 0) return $"{h}h {m}m";
        if (h > 0) return $"{h}h";
        if (m > 0 && s > 0) return $"{m}m {s}s";
        if (m > 0) return $"{m}m";
        return $"{s}s";
    }

    public static void Execute(string query)
    {
        if (TryParseDuration(query.Trim(), out var totalSeconds))
        {
            _ = StartTimerAsync(totalSeconds);
        }
    }

    private static async Task StartTimerAsync(int seconds)
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        // Notification handled by UI layer
    }

    public static bool TryParse(string query, out TimeSpan duration)
    {
        if (TryParseDuration(query.Trim(), out var totalSeconds))
        {
            duration = TimeSpan.FromSeconds(totalSeconds);
            return true;
        }
        duration = TimeSpan.Zero;
        return false;
    }
}
