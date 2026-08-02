using System.Threading.Channels;
using Spur.Models;
using Spur.Plugin;
using Spur.Services;
using Query = Spur.Plugin.Query;

namespace Spur.Plugins;

/// <summary>Wraps the existing SearchEngineService as an IPlugin.</summary>
public sealed class LegacySearchAdapter : IPlugin
{
    public string Id => "legacy-search";
    public string Name => "Search";
    public string Description => "Built-in search providers";
    public string IconGlyph => "\uE721";
    public string ActionKeyword => string.Empty;

    private readonly ISearchEngineService _inner;

    public LegacySearchAdapter(ISearchEngineService inner) => _inner = inner;

    public Task InitAsync(PluginInitContext context, CancellationToken ct = default) => Task.CompletedTask;

    public async Task<List<PluginResult>> QueryAsync(Query query, CancellationToken ct)
    {
        var channel = Channel.CreateUnbounded<IResultItem>();
        var writer = channel.Writer;

        // Run the existing search engine — it writes SearchResult items to the channel
        var searchTask = _inner.SearchAsync(query.Search, null, writer, ct);

        // Read all results from the channel
        var reader = channel.Reader;
        var items = new List<IResultItem>();
        await foreach (var item in reader.ReadAllAsync(ct))
            items.Add(item);

        await searchTask;

        // Map IResultItem → PluginResult
        var results = new List<PluginResult>();
        foreach (var item in items)
        {
            if (item is not SearchResult sr) continue;
            results.Add(new PluginResult
            {
                Id = sr.Id,
                Title = sr.Name,
                Subtitle = sr.Subtitle,
                Section = Name,
                IconGlyph = sr.IconGlyph,
                IconPath = sr.IconPath ?? string.Empty,
                Score = (int)sr.Score,
                Source = sr,
            });
        }

        return results;
    }
}
