using System.Collections.ObjectModel;
using System.Threading.Channels;
using Spur.Models;
using Spur.Extensions;

namespace Spur.Services;

public interface ISearchEngineService
{
    Task SearchAsync(string query, string? activeCategory, ChannelWriter<IResultItem> writer, CancellationToken ct);
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

    public async Task SearchAsync(string query, string? activeCategory, ChannelWriter<IResultItem> writer, CancellationToken ct)
    {
        try
        {
            if (activeCategory is not null)
            {
                await WriteScopedResultsAsync(query, activeCategory, writer, ct);
                return;
            }

            await WriteParallelResultsAsync(query, writer, ct);
        }
        catch (OperationCanceledException) { }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task WriteScopedResultsAsync(string query, string activeCategory, ChannelWriter<IResultItem> writer, CancellationToken ct)
    {
        if (activeCategory == "actions")
        {
            var actionCatalog = BuildActionCatalog(query).ToList();
            if (actionCatalog.Count > 0)
            {
                foreach (var r in actionCatalog)
                    await writer.WriteAsync(r, ct);
            }
            return;
        }

        var scopedExtra = _addOns.FindById(activeCategory);
        if (scopedExtra is not null)
        {
            var actionResults = scopedExtra.GetResults(query).ToList();
            if (actionResults.Count > 0)
            {
                foreach (var r in actionResults)
                    await writer.WriteAsync(r, ct);
            }
        }
    }

    private async Task WriteParallelResultsAsync(string query, ChannelWriter<IResultItem> writer, CancellationToken ct)
    {
        int resultCount = 0;

        var tasks = new List<Task>();

        if (_config.IndexApps && _appCatalog.Count > 0)
            tasks.Add(RunSourceAsync(0, async (w, t) =>
            {
                var items = SearchApps(query);
                if (items.Count == 0) return;
                foreach (var r in items)
                {
                    await w.WriteAsync(r, t);
                    Interlocked.Increment(ref resultCount);
                }
            }, writer, ct));

        if (_config.FileSearchEnabled)
            tasks.Add(RunSourceAsync(80, async (w, t) =>
            {
                int limit = _config.ResultsCount;
                var items = string.IsNullOrEmpty(query)
                    ? await _files.BrowseRecentAsync(limit)
                    : await _files.SearchAsync(query, limit, t);
                if (items.Count == 0) return;
                foreach (var r in items)
                {
                    await w.WriteAsync(r, t);
                    Interlocked.Increment(ref resultCount);
                }
            }, writer, ct));

        if (_config.ClipboardEnabled && _config.IndexClipboard)
            tasks.Add(RunSourceAsync(0, async (w, t) =>
            {
                var items = SearchClipboard(query, 3);
                if (items.Count == 0) return;
                foreach (var r in items)
                {
                    await w.WriteAsync(r, t);
                    Interlocked.Increment(ref resultCount);
                }
            }, writer, ct));

        tasks.Add(RunSourceAsync(0, async (w, t) =>
        {
            var items = SearchGlobalActions(query);
            if (items.Count == 0) return;
            foreach (var r in items)
            {
                await w.WriteAsync(r, t);
                Interlocked.Increment(ref resultCount);
            }
        }, writer, ct));

        if (_config.IndexWindowsSettings && !string.IsNullOrEmpty(query))
            tasks.Add(RunSourceAsync(0, async (w, t) =>
            {
                var items = SearchSettings(query);
                if (items.Count == 0) return;
                foreach (var r in items)
                {
                    await w.WriteAsync(r, t);
                    Interlocked.Increment(ref resultCount);
                }
            }, writer, ct));

        await Task.WhenAll(tasks).ConfigureAwait(false);
        if (ct.IsCancellationRequested) return;

        if (resultCount < 3)
        {
            var web = BuildWebSearchAction(query);
            if (web is not null)
                await writer.WriteAsync(web, ct);
        }
    }

    private static async Task RunSourceAsync(
        int staggerMs,
        Func<ChannelWriter<IResultItem>, CancellationToken, Task> source,
        ChannelWriter<IResultItem> writer,
        CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;
        if (staggerMs > 0)
        {
            try { await Task.Delay(staggerMs, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }
        }
        await source(writer, ct).ConfigureAwait(false);
    }

    private List<SearchResult> SearchApps(string query)
    {
        var matches = new List<SearchResult>(_appCatalog.Count);
        foreach (var a in _appCatalog)
        {
            var clone = Clone(a);
            var score = MatchScore(query, a.Name, clone);
            if (score < 0) continue;
            var freqBoost = Math.Log2(a.FrequencyScore + 1) * 0.5;
            clone.Score = score + freqBoost;
            if (_config.PinnedItems.Contains(a.Id))
            {
                clone.IsPinned = true;
                clone.Score += 10000;
            }
            matches.Add(clone);
        }
        matches.Sort((x, y) => y.Score.CompareTo(x.Score));
        return matches.Take(_config.ResultsCount).ToList();
    }

    private List<SearchResult> SearchClipboard(string query, int limit)
    {
        var clips = new List<SearchResult>(limit);
        foreach (var c in _clipboard.GetHistory())
        {
            if (clips.Count >= limit) break;
            var sr = new SearchResult
            {
                Id = $"clip:{c.Timestamp.Ticks}",
                Type = ResultType.Clipboard,
                Name = c.Preview,
                Subtitle = c.TimeAgo,
                IconGlyph = c.IsImage ? "image" : "clipboard",
                IconPath = "/Assets/Icons/copy.png",
                ClipContent = c.Content,
                ClipTimestamp = c.Timestamp,
                ClipImage = c.Image,
            };
            if (!string.IsNullOrEmpty(query) && MatchScore(query, c.Preview, sr) < 0) continue;
            if (_config.PinnedItems.Contains(sr.Id))
            {
                sr.IsPinned = true;
                sr.Score = 10000;
            }
            clips.Add(sr);
        }
        clips.Sort((x, y) => y.IsPinned.CompareTo(x.IsPinned));
        return clips;
    }

    private List<SearchResult> SearchGlobalActions(string query)
    {
        var results = new List<SearchResult>();
        foreach (var extra in _addOns.GetGlobalEnabled())
        {
            if (!extra.CanHandle(query)) continue;
            var r = extra.BuildResult(query);
            r.Score = MatchScore(query, r.Name, r);
            results.Add(r);
        }
        foreach (var extra in _addOns.GetEnabled())
        {
            if (extra.IsGlobal) continue;
            if (string.IsNullOrWhiteSpace(query)) continue;
            if (!extra.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !extra.Keyword.Contains(query, StringComparison.OrdinalIgnoreCase)
                && !(extra.Id == "ai" && "ask ai".Contains(query, StringComparison.OrdinalIgnoreCase)))
                continue;
            results.Add(new SearchResult
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
        if (url is not null) results.Add(url);
        results.Sort((x, y) => y.Score.CompareTo(x.Score));
        return results;
    }

    private List<SearchResult> SearchSettings(string query)
    {
        var matches = new List<SearchResult>(SpurConstants.WindowsSettings.Length);
        foreach (var s in SpurConstants.WindowsSettings)
        {
            var c = Clone(s);
            var sc = MatchScore(query, s.Name, c);
            if (sc < 0) continue;
            c.Score = sc;
            matches.Add(c);
        }
        matches.Sort((x, y) => y.Score.CompareTo(x.Score));
        if (matches.Count > 4) matches.RemoveRange(4, matches.Count - 4);
        return matches;
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

    private double MatchScore(string query, string target, SearchResult? result = null)
    {
        if (string.IsNullOrEmpty(query)) return 0;
        if (!_config.FuzzySearch)
            return target.Contains(query, StringComparison.OrdinalIgnoreCase) ? 1 : -1;

        var match = FuzzySearch.Match(query, target);
        if (result is not null && match.Success)
            result.TitleHighlightData = match.MatchedIndices as HashSet<int>;
        return match.Score >= MinMatchScore() ? match.Score : -1;
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

    private static bool ShouldOfferWebFallback(string query, List<IResultItem> results)
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
