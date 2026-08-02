using Spur.Models;
using Spur.Plugin;
using Spur.Services;
using Query = Spur.Plugin.Query;

namespace Spur.Plugins;

public sealed class AppsPlugin : IPlugin
{
    public string Id => "apps";
    public string Name => "Applications";
    public string Description => "Installed applications from Start Menu, Desktop, and PATH";
    public string IconGlyph => "\uE734";
    public string ActionKeyword => string.Empty;

    private readonly IAppDiscoveryService _apps;
    private readonly IFrequencyService _freq;
    private readonly SpurConfig _config;
    private IReadOnlyList<SearchResult> _catalog = [];

    public AppsPlugin(IAppDiscoveryService apps, IFrequencyService freq, SpurConfig config)
    {
        _apps = apps;
        _freq = freq;
        _config = config;
    }

    public Task InitAsync(PluginInitContext context, CancellationToken ct = default)
    {
        _apps.CatalogRefreshed += catalog =>
        {
            _catalog = catalog;
            foreach (var app in _catalog)
                if (app.ExePath is not null)
                    app.FrequencyScore = _freq.Get(app.ExePath);
        };
        return Task.CompletedTask;
    }

    public Task<List<PluginResult>> QueryAsync(Query query, CancellationToken ct)
    {
        if (!_config.IndexApps || _catalog.Count == 0)
            return Task.FromResult(new List<PluginResult>());

        var q = query.Search;
        var matches = new List<SearchResult>(_catalog.Count);
        foreach (var a in _catalog)
        {
            var clone = PluginHelper.Clone(a);
            var score = PluginHelper.MatchScore(q, a.Name, _config, clone);
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
        matches = matches.Take(_config.ResultsCount).ToList();

        return Task.FromResult(matches.Select(m => PluginHelper.ToPluginResult(m, Name)).ToList());
    }
}
