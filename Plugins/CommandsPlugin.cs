using Spur.Models;
using Spur.Plugin;
using Spur.Services;
using Spur.Extensions;
using Query = Spur.Plugin.Query;

namespace Spur.Plugins;

public sealed class CommandsPlugin : IPlugin
{
    public string Id => "commands";
    public string Name => "Commands";
    public string Description => "Global actions, add-ons, URL detection";
    public string IconGlyph => "\uE156";
    public string ActionKeyword => string.Empty;

    private readonly AddOnRegistry _addOns;
    private readonly SpurConfig _config;

    public CommandsPlugin(AddOnRegistry addOns, SpurConfig config)
    {
        _addOns = addOns;
        _config = config;
    }

    public Task InitAsync(PluginInitContext context, CancellationToken ct = default) => Task.CompletedTask;

    public Task<List<PluginResult>> QueryAsync(Query query, CancellationToken ct)
    {
        var q = query.Search;
        var results = new List<PluginResult>();

        foreach (var extra in _addOns.GetGlobalEnabled())
        {
            if (!extra.CanHandle(q)) continue;
            var r = extra.BuildResult(q);
            r.Score = PluginHelper.MatchScore(q, r.Name, _config, r);
            results.Add(new PluginResult
            {
                Id = r.Id,
                Title = r.Name,
                Subtitle = r.Subtitle,
                Section = Name,
                IconGlyph = r.IconGlyph,
                IconPath = r.IconPath ?? string.Empty,
                Score = (int)r.Score,
                Source = r,
            });
        }

        foreach (var extra in _addOns.GetEnabled())
        {
            if (extra.IsGlobal) continue;
            if (string.IsNullOrWhiteSpace(q)) continue;
            if (!extra.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                && !extra.Keyword.Contains(q, StringComparison.OrdinalIgnoreCase)
                && !(extra.Id == "ai" && "ask ai".Contains(q, StringComparison.OrdinalIgnoreCase)))
                continue;
            results.Add(new PluginResult
            {
                Id = $"action-catalog:{extra.Id}",
                Title = extra.Name,
                Subtitle = extra.Description,
                Section = Name,
                IconGlyph = extra.IconGlyph,
                IconPath = extra.IconPath ?? string.Empty,
                Score = 500,
            });
        }

        if (_config.IndexUrls && PluginHelper.LooksLikeUrl(q))
        {
            results.Add(new PluginResult
            {
                Id = $"url:{q}",
                Title = $"Open {q}",
                Subtitle = "Open URL",
                Section = Name,
                IconGlyph = "\ue12b",
                IconPath = "/Assets/Icons/url.png",
                Score = 500,
            });
        }

        results.Sort((x, y) => y.Score.CompareTo(x.Score));
        return Task.FromResult(results);
    }
}
