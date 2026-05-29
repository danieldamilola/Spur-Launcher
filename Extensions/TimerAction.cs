namespace Arc.Extensions;

/// <summary>
/// Timer action. Triggered by "timer 10m", "timer 30s", "timer 1h".
/// The actual countdown runs in the PreviewPanel / MainViewModel.
/// </summary>
public sealed class TimerAction : IAction
{
    public string Id => "timer";

    private static readonly Regex _trigger = new(
        @"^timer\s+\d+[smh]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanHandle(string query) =>
        !string.IsNullOrWhiteSpace(query) && _trigger.IsMatch(query.Trim());

    public SearchResult BuildResult(string query)
    {
        TryParse(query, out var duration);
        var label = FormatDuration(duration);

        return new SearchResult
        {
            Id       = "action:timer",
            Type     = ResultType.Action,
            Name     = $"Timer · {label}",
            Subtitle = "Press ↵ to start",
            LucideIcon = "timer",
            ActionId = Id,
        };
    }

    /// <summary>Parses "timer {n}{s|m|h}" and returns the TimeSpan.</summary>
    public static bool TryParse(string query, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        var m = Regex.Match(query.Trim(),
            @"^timer\s+(\d+)([smh])$", RegexOptions.IgnoreCase);

        if (!m.Success || !int.TryParse(m.Groups[1].Value, out var n) || n <= 0)
            return false;

        duration = m.Groups[2].Value.ToLowerInvariant() switch
        {
            "s" => TimeSpan.FromSeconds(n),
            "m" => TimeSpan.FromMinutes(n),
            "h" => TimeSpan.FromHours(n),
            _   => TimeSpan.Zero,
        };
        return duration > TimeSpan.Zero;
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m";
        if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m";
        return $"{(int)ts.TotalSeconds}s";
    }
}

