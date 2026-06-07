using System.Collections.ObjectModel;
using Spur.Models;
using Spur.Extensions;

namespace Spur.Services;

public interface ISearchEngineService
{
    Task<List<object>> SearchAsync(string query, string? activeCategory, CancellationToken ct);
}

public sealed class SearchEngineService : ISearchEngineService
{
    private readonly ILogger _log;
    private readonly IAppDiscoveryService _apps;
    private readonly IFileSearchService _files;
    private readonly IClipboardService _clipboard;
    private readonly SpurConfig _config;
    private readonly IFrequencyService _freq;

    private volatile IReadOnlyList<SearchResult> _appCatalog = [];

    private static readonly IAction[] Actions =
    [
        new CalculatorAction(),
        new ColorAction(),
        new TimerAction(),
        new IpAction(),
        new AiAction(),
        new SettingsAction(),
        new SystemAction(),
        new CurrencyAction(),
        new PasswordGenAction(),
        new QuickNoteAction(),
        new KillProcessAction(),
        new ScreenshotAction(),
        new ShellAction(),
    ];

    public SearchEngineService(
        ILogger log,
        IAppDiscoveryService apps,
        IFileSearchService files,
        IClipboardService clipboard,
        SpurConfig config,
        IFrequencyService freq)
    {
        _log = log;
        _apps = apps;
        _files = files;
        _clipboard = clipboard;
        _config = config;
        _freq = freq;

        _apps.CatalogRefreshed += HandleCatalogRefreshed;
    }

    private void HandleCatalogRefreshed(List<SearchResult> freshCatalog)
    {
        _appCatalog = freshCatalog;
        foreach (var app in _appCatalog)
        {
            if (app.ExePath is not null)
                app.FrequencyScore = _freq.Get(app.ExePath);
        }
    }

    public async Task<List<object>> SearchAsync(string query, string? activeCategory, CancellationToken ct)
    {
        var newResults = new List<object>();
        bool isBrowseMode = activeCategory is not null;

        // -- Keyword-scoped action (e.g. "sys ", "timer ") ----------
        if (activeCategory is not null)
        {
            if (activeCategory == "actions")
            {
                var actionCatalog = BuildActionCatalog(query).ToList();
                if (actionCatalog.Count > 0)
                {
                    newResults.Add(new SectionLabel("Commands"));
                    newResults.AddRange(actionCatalog);
                }
                return newResults;
            }

            var scopedAction = Actions.FirstOrDefault(a => a.Id == activeCategory && !a.IsGlobal);
            if (scopedAction is not null)
            {
                if (scopedAction.Id != "ai" && !IsActionEnabled(scopedAction.Id)) return newResults;
                ApplyActionSettings(scopedAction.Id);
                var actionResults = scopedAction.GetResults(query).ToList();
                if (actionResults.Count > 0)
                {
                    newResults.Add(new SectionLabel(scopedAction.Name));
                    newResults.AddRange(actionResults);
                }
                return newResults;
            }
        }

        // -- Apps --------------------------------------------------
        bool showApps = _config.IndexApps && (activeCategory is null or "apps");
        if (showApps && _appCatalog is not null)
        {
            var appMatches = new List<SearchResult>(_appCatalog.Count);
            foreach (var a in _appCatalog)
            {
                var score = MatchScore(query, a.Name);
                if (score < 0) continue;
                var clone = Clone(a);
                var freqBoost = Math.Log2(a.FrequencyScore + 1) * 0.5;
                clone.Score = score + freqBoost;
                if (_config.PinnedItems.Contains(a.Id))
                {
                    clone.IsPinned = true;
                    clone.Score += 10000;
                }
                appMatches.Add(clone);
            }
            appMatches.Sort((x, y) => y.Score.CompareTo(x.Score));

            var appList = isBrowseMode
                ? appMatches
                : appMatches.Take(_config.ResultsCount).ToList();

            if (appList.Count > 0)
            {
                newResults.Add(new SectionLabel("Applications"));
                newResults.AddRange(appList);
            }
        }

        if (ct.IsCancellationRequested) return newResults;

        // -- Inline answers (global search only) --------------------
        if (activeCategory is null)
        {
            foreach (var inline in BuildInlineResults(query))
                newResults.Add(inline);
        }

        // -- Files -------------------------------------------------
        bool showFiles = _config.FileSearchEnabled && (activeCategory is null or "files");
        if (showFiles)
        {
            int fileLimit = isBrowseMode ? 50 : _config.ResultsCount;

            List<SearchResult> fileMatches;
            if (string.IsNullOrEmpty(query) && activeCategory == "files")
            {
                fileMatches = await _files.BrowseRecentAsync(fileLimit);
            }
            else
            {
                fileMatches = await _files.SearchAsync(query, fileLimit, ct);
            }

            if (ct.IsCancellationRequested) return newResults;
            if (fileMatches.Count > 0)
            {
                newResults.Add(new SectionLabel("Files"));
                newResults.AddRange(fileMatches);
            }
        }

        // -- Clipboard ---------------------------------------------
        bool showClip = _config.ClipboardEnabled && _config.IndexClipboard && activeCategory == "clipboard";
        if (showClip)
        {
            int limit = isBrowseMode ? 50 : _config.ResultsCount;
            var clips = new List<SearchResult>(limit);
            foreach (var c in _clipboard.GetHistory())
            {
                if (clips.Count >= limit) break;
                if (!string.IsNullOrEmpty(query) && MatchScore(query, c.Preview) < 0) continue;

                var sr = new SearchResult
                {
                    Id         = $"clip:{c.Timestamp.Ticks}",
                    Type       = ResultType.Clipboard,
                    Name       = c.Preview,
                    Subtitle   = c.TimeAgo,
                    IconGlyph = c.IsImage ? "image" : "clipboard",
                    ClipContent = c.Content,
                    ClipTimestamp = c.Timestamp,
                    ClipImage = c.Image,
                };
                if (_config.PinnedItems.Contains(sr.Id))
                {
                    sr.IsPinned = true;
                    sr.Score = 10000;
                }
                clips.Add(sr);
            }
            clips.Sort((x, y) => y.IsPinned.CompareTo(x.IsPinned));

            if (clips.Count > 0)
            {
                newResults.Add(new SectionLabel("Clipboard"));
                newResults.AddRange(clips);
            }
        }

        // -- Global actions (global search only) --------------------
        if (activeCategory is null)
        {
            var globalActionResults = new List<SearchResult>();

            foreach (var action in Actions)
            {
                if (!action.IsGlobal || !IsActionEnabled(action.Id) || !action.CanHandle(query)) continue;
                var r = action.BuildResult(query);
                r.Score = MatchScore(query, r.Name);
                globalActionResults.Add(r);
            }

            var url = BuildUrlAction(query);
            if (url is not null) globalActionResults.Add(url);

            var shell = BuildShellAction(query);
            if (shell is not null) globalActionResults.Add(shell);

            globalActionResults.Sort((x, y) => y.Score.CompareTo(x.Score));

            if (globalActionResults.Count > 0)
            {
                newResults.Add(new SectionLabel("Commands"));
                newResults.AddRange(globalActionResults);
            }
        }

        // -- Windows Settings --------------------------------------
        if (_config.IndexWindowsSettings && !string.IsNullOrEmpty(query) && (activeCategory is null or "apps"))
        {
            var settingsMatches = new List<SearchResult>(SpurConstants.WindowsSettings.Length);
            foreach (var s in SpurConstants.WindowsSettings)
            {
                var sc = MatchScore(query, s.Name);
                if (sc < 0) continue;
                var c = Clone(s); c.Score = sc;
                settingsMatches.Add(c);
            }
            settingsMatches.Sort((x, y) => y.Score.CompareTo(x.Score));
            if (settingsMatches.Count > 4)
                settingsMatches.RemoveRange(4, settingsMatches.Count - 4);

            if (settingsMatches.Count > 0)
            {
                newResults.Add(new SectionLabel("Settings"));
                newResults.AddRange(settingsMatches);
            }
        }

        if (ct.IsCancellationRequested) return newResults;

        if (activeCategory is null && ShouldOfferWebFallback(query, newResults))
        {
            var web = BuildWebSearchAction(query);
            if (web is not null)
            {
                newResults.Add(new SectionLabel("Web"));
                newResults.Add(web);
            }
        }

        return newResults;
    }

    private IEnumerable<SearchResult> BuildActionCatalog(string query)
    {
        var entries = new[]
        {
            ("system", "System commands", _config.KeywordSystem, "Shutdown, restart, sleep, lock, sign out.", "power"),
            ("color", "Color tools", _config.KeywordColor, "Convert and copy hex colors.", "\ue790"),
            ("currency", "Currency converter", _config.KeywordCurrency, "Convert an amount between currencies.", "\ue825"),
            ("timer", "Timer", _config.KeywordTimer, "Start a countdown from Spur.", "\ue121"),
            ("ai", "AI assistant", _config.KeywordAi, "Ask the configured AI provider.", "AI"),
            ("ip", "IP tools", _config.KeywordIp, "Show local and public IP addresses.", "\ue701"),
            ("pw", "Password generator", _config.KeywordPassword, "Generate and copy a password.", "\ue722"),
            ("note", "Quick note", _config.KeywordNote, "Save a short note.", "\ue727"),
            ("kill", "Kill process", _config.KeywordKill, "Find and terminate running processes.", "\ue747"),
            ("screenshot", "Screenshot", _config.KeywordScreenshot, "Capture and save the screen.", "\ue74c"),
            ("shell", "Shell commands", _config.KeywordShell, "Run a command through the configured terminal.", "\ue765"),
        };

        foreach (var (id, name, keyword, description, icon) in entries)
        {
            if (!IsActionEnabled(id)) continue;
            if (!string.IsNullOrWhiteSpace(query)
                && !name.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !keyword.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !description.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return new SearchResult
            {
                Id = $"action-catalog:{id}",
                Type = ResultType.Action,
                Name = name,
                Subtitle = string.IsNullOrWhiteSpace(keyword) ? description : $"{keyword}  ·  {description}",
                IconGlyph = icon,
                ActionId = id,
                Score = 500,
            };
        }
    }

    private void ApplyActionSettings(string actionId)
    {
        if (actionId == "timer")
            TimerAction.PresetText = _config.Timer.DefaultPresets;
        else if (actionId == "kill")
        {
            KillProcessAction.ShowWindowTitles = _config.KillProcess.ShowWindowTitles;
            KillProcessAction.PrioritizeVisibleWindows = _config.KillProcess.PrioritizeVisibleWindows;
        }
    }

    private double MatchScore(string query, string target)
    {
        if (string.IsNullOrEmpty(query)) return 0;
        var score = _config.FuzzySearch
            ? FuzzySearch.Score(query, target)
            : (target.Contains(query, StringComparison.OrdinalIgnoreCase) ? 1 : -1);
        return score >= MinMatchScore() ? score : -1;
    }

    private double MinMatchScore() => _config.QuerySearchPrecision switch
    {
        "low" => 0,
        "strict" => 1.2,
        _ => 0.35,
    };

    private bool IsActionEnabled(string? id) => id switch
    {
        "calc"       => _config.IndexCalculator,
        "color"      => _config.ActionColor,
        "timer"      => _config.ActionTimer,
        "ip"         => _config.ActionIp,
        "ai"         => _config.ActionAi,
        "currency"   => _config.ActionCurrency,
        "pw"         => _config.ActionPasswordGen,
        "note"       => _config.ActionQuickNote,
        "kill"       => _config.ActionKillProcess,
        "screenshot" => _config.ActionScreenshot,
        "system"     => _config.IndexSystemCommands,
        "settings"   => _config.IndexWindowsSettings,
        "url"        => _config.IndexUrls,
        "web"        => _config.IndexWebSearches,
        "shell"      => _config.IndexShell,
        _            => true,
    };

    private SearchResult? BuildUrlAction(string query)
    {
        if (!_config.IndexUrls || !LooksLikeUrl(query)) return null;
        return new SearchResult
        {
            Id = $"url:{query}",
            Type = ResultType.Action,
            Name = $"Open {query}",
            Subtitle = "Open URL",
            IconGlyph = "\ue12b",
            ActionId = "url",
            Score = 500,
        };
    }

    private SearchResult? BuildWebSearchAction(string query)
    {
        var q = NormalizeWebQuery(query);
        if (!_config.IndexWebSearches || string.IsNullOrWhiteSpace(q)) return null;
        return new SearchResult
        {
            Id = $"web:{q}",
            Type = ResultType.Action,
            Name = $"Search the web for \"{q}\"",
            Subtitle = "Web",
            IconGlyph = "\ue11a",
            ActionId = "web",
            Score = 50,
        };
    }

    private SearchResult? BuildShellAction(string query)
    {
        if (!_config.IndexShell) return null;
        var command = ExtractShellCommand(query);
        if (command.Length == 0) return null;
        return new SearchResult
        {
            Id = $"shell:{command}",
            Type = ResultType.Action,
            Name = $"Run {command}",
            Subtitle = "Shell command",
            IconGlyph = "\ue765",
            ActionId = "shell",
            Score = 600,
        };
    }

    private string ExtractShellCommand(string query)
    {
        var keyword = _config.KeywordShell;
        var trimmed = query.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) return string.Empty;

        if (keyword == ">" && trimmed.StartsWith(">", StringComparison.Ordinal))
            return trimmed[1..].Trim();

        var prefix = keyword + " ";
        return trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? trimmed[prefix.Length..].Trim()
            : string.Empty;
    }

    private static bool LooksLikeUrl(string query)
        => Uri.TryCreate(NormalizeUrl(query), UriKind.Absolute, out var uri)
           && uri.Scheme is "http" or "https";

    private static string NormalizeWebQuery(string query)
    {
        var q = query.Trim();
        return q.StartsWith('?') ? q[1..].Trim() : q;
    }

    private IEnumerable<SearchResult> BuildInlineResults(string query)
    {
        foreach (var action in Actions)
        {
            if (!action.IsGlobal || !IsActionEnabled(action.Id) || !action.CanHandle(query)) continue;
            yield return action.BuildResult(query);
        }

        var url = BuildUrlAction(query);
        if (url is not null)
            yield return url;
    }

    private static bool ShouldOfferWebFallback(string query, List<object> results)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;
        int localCount = 0;
        foreach (var r in results)
        {
            if (r is SearchResult) localCount++;
        }
        return localCount < 3;
    }

    private static string NormalizeUrl(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Contains("://", StringComparison.Ordinal)
            ? trimmed
            : $"https://{trimmed}";
    }

    private static SearchResult Clone(SearchResult s) => new()
    {
        Id = s.Id, Name = s.Name, Subtitle = s.Subtitle, Type = s.Type,
        ExePath = s.ExePath, IconPath = s.IconPath, IconGlyph = s.IconGlyph,
        ActionId = s.ActionId, FrequencyScore = s.FrequencyScore
    };
}
