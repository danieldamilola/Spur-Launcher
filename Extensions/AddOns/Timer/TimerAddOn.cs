using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.AddOns.Timer;

public sealed class TimerAddOn : IAddOn
{
    public string Id => "timer";
    public string Name => "Timer";
    public string Description => "Start a countdown directly from the launcher.";
    public string IconGlyph => "\ue121";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "timer";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView()
    {
        return new TimerSettingsView { DataContext = Settings };
    }

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
                    IconGlyph  = IconGlyph,
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
                IconGlyph  = IconGlyph,
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
                IconGlyph  = IconGlyph,
                ActionId   = Id,
            };
        }
    }

    private IEnumerable<(string label, string input, int seconds)> GetPresets()
    {
        var presetText = ((TimerSettings?)Settings)?.DefaultPresets ?? "1m,3m,5m,10m,15m,30m";
        var yielded = false;
        foreach (var token in presetText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
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

    public bool CanHandle(string query) => false;

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
                IconGlyph  = IconGlyph,
                ActionId   = Id,
            };
        }
        return new() { Id = "action:timer", Type = ResultType.Action, Name = Name, ActionId = Id };
    }

    public static bool TryParseDuration(string input, out int totalSeconds)
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

    public static string FormatDuration(int totalSeconds)
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

    public Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        return Task.FromResult(new AddOnResult
        {
            Success = true,
            Title = Name,
            Detail = input,
            PanelId = Id
        });
    }
}
