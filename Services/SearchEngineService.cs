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
            var scopedAction = Actions.FirstOrDefault(a => a.Id == activeCategory && !a.IsGlobal);
            if (scopedAction is not null)
            {
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
            var appMatches = _appCatalog
                .Select(a =>
                {
                    var score = MatchScore(query, a.Name);
                    if (score < 0) return null;
                    var clone = Clone(a);
                    var freqBoost = Math.Log2(a.FrequencyScore + 1) * 0.5;
                    clone.Score = score + freqBoost;
                    if (_config.PinnedItems.Contains(a.Id))
                    {
                        clone.IsPinned = true;
                        clone.Score += 10000;
                    }
                    return clone;
                })
                .Where(a => a is not null)
                .Cast<SearchResult>()
                .OrderByDescending(a => a.Score);

            var appList = isBrowseMode
                ? appMatches.ToList()
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
            var clips = _clipboard.GetHistory()
                .Where(c => string.IsNullOrEmpty(query) || MatchScore(query, c.Preview) >= 0)
                .Take(limit)
                .Select(c => new SearchResult
                {
                    Id         = $"clip:{c.Timestamp.Ticks}",
                    Type       = ResultType.Clipboard,
                    Name       = c.Preview,
                    Subtitle   = c.TimeAgo,
                    IconGlyph = c.IsImage ? "image" : "clipboard",
                    ClipContent = c.Content,
                    ClipTimestamp = c.Timestamp,
                    ClipImage = c.Image,
                })
                .Select(c =>
                {
                    if (_config.PinnedItems.Contains(c.Id))
                    {
                        c.IsPinned = true;
                        c.Score = 10000;
                    }
                    return c;
                })
                .OrderByDescending(c => c.IsPinned)
                .ToList();

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

            foreach (var action in Actions.Where(a => a.IsGlobal && IsActionEnabled(a.Id) && a.CanHandle(query)))
            {
                var r = action.BuildResult(query);
                r.Score = MatchScore(query, r.Name);
                globalActionResults.Add(r);
            }

            var url = BuildUrlAction(query);
            if (url is not null) globalActionResults.Add(url);

            var shell = BuildShellAction(query);
            if (shell is not null) globalActionResults.Add(shell);

            globalActionResults = globalActionResults.OrderByDescending(r => r.Score).ToList();

            if (globalActionResults.Count > 0)
            {
                newResults.Add(new SectionLabel("Commands"));
                newResults.AddRange(globalActionResults);
            }
        }

        // -- Windows Settings --------------------------------------
        if (_config.IndexWindowsSettings && !string.IsNullOrEmpty(query) && (activeCategory is null or "apps"))
        {
            var settingsMatches = SpurConstants.WindowsSettings
                .Select(s =>
                {
                    var sc = MatchScore(query, s.Name);
                    if (sc < 0) return null;
                    var c = Clone(s); c.Score = sc; return c;
                })
                .Where(s => s is not null)
                .Cast<SearchResult>()
                .OrderByDescending(s => s.Score)
                .Take(4)
                .ToList();

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
        if (!_config.IndexShell || !query.StartsWith(">", StringComparison.Ordinal)) return null;
        var command = query[1..].Trim();
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
        foreach (var action in Actions.Where(a => a.IsGlobal && IsActionEnabled(a.Id) && a.CanHandle(query)))
            yield return action.BuildResult(query);

        var url = BuildUrlAction(query);
        if (url is not null)
            yield return url;
    }

    private static bool ShouldOfferWebFallback(string query, List<object> results)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;
        var localCount = results.OfType<SearchResult>().Count();
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
