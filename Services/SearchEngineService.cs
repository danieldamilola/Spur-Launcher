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
    private readonly AddOnRegistry _addOns;

    private volatile IReadOnlyList<SearchResult> _appCatalog = [];

    public SearchEngineService(
        ILogger log,
        IAppDiscoveryService apps,
        IFileSearchService files,
        IClipboardService clipboard,
        SpurConfig config,
        IFrequencyService freq,
        AddOnRegistry addOns)
    {
        _log = log;
        _apps = apps;
        _files = files;
        _clipboard = clipboard;
        _config = config;
        _freq = freq;
        _addOns = addOns;

        _addOns.LoadSettings(_config);

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

            var scopedExtra = _addOns.FindById(activeCategory);
            if (scopedExtra is not null)
            {
                var actionResults = scopedExtra.GetResults(query).ToList();
                if (actionResults.Count > 0)
                {
                    newResults.Add(new SectionLabel(scopedExtra.Name));
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
                IconPath = "/Assets/Icons/copy.png",
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

            foreach (var extra in _addOns.GetGlobalEnabled())
            {
                if (!extra.CanHandle(query)) continue;
                var r = extra.BuildResult(query);
                r.Score = MatchScore(query, r.Name);
                globalActionResults.Add(r);
            }

            // Also include non-global add-ons when query matches their name/keyword
            foreach (var extra in _addOns.GetEnabled())
            {
                if (extra.IsGlobal) continue;
                if (string.IsNullOrWhiteSpace(query)) continue;
                if (!extra.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                    && !extra.Keyword.Contains(query, StringComparison.OrdinalIgnoreCase)
                    && !(extra.Id == "ai" && "ask ai".Contains(query, StringComparison.OrdinalIgnoreCase)))
                    continue;

                globalActionResults.Add(new SearchResult
                {
                    Id = $"action-catalog:{extra.Id}",
                    Type = ResultType.Action,
                    Name = extra.Name,
                    Subtitle = extra.Description,
                    IconGlyph = extra.IconGlyph,
                    IconPath = extra.IconPath,
                    ActionId = extra.Id,
                    Score = 500,
                });
            }

            var url = BuildUrlAction(query);
            if (url is not null) globalActionResults.Add(url);

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
        foreach (var extra in _addOns.GetEnabled())
        {
            if (extra.IsGlobal) continue; // Global extras don't have a keyword-scoped entry in the catalog usually, though calc is an exception. Actually calc has IsGlobal=true but shouldn't show up here normally. We'll skip globals.

            if (!string.IsNullOrWhiteSpace(query)
                && !extra.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !extra.Keyword.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !extra.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return new SearchResult
            {
                Id = $"action-catalog:{extra.Id}",
                Type = ResultType.Action,
                Name = extra.Name,
                Subtitle = string.IsNullOrWhiteSpace(extra.Keyword) ? extra.Description : $"{extra.Keyword}  ·  {extra.Description}",
                IconGlyph = extra.IconGlyph,
                IconPath = extra.IconPath,
                ActionId = extra.Keyword, // Set ActionId to keyword so MainViewModel can use it
                Score = 500,
            };
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
            IconPath = "/Assets/Icons/url.png",
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
            IconPath = "/Assets/Icons/search.png",
            ActionId = "web",
            Score = 50,
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
        ExePath = s.ExePath, LnkPath = s.LnkPath, IconPath = s.IconPath, IconGlyph = s.IconGlyph,
        FilePath = s.FilePath, FileExtension = s.FileExtension, IsDirectory = s.IsDirectory,
        ClipContent = s.ClipContent, ClipTimestamp = s.ClipTimestamp, ClipImage = s.ClipImage,
        ActionId = s.ActionId, FrequencyScore = s.FrequencyScore, IsPinned = s.IsPinned,
    };
}
