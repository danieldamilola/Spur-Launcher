using Spur.Plugin;
using Query = Spur.Plugin.Query;

namespace Spur.Plugins;

public sealed class WebPlugin : IPlugin
{
    public string Id => "web";
    public string Name => "Web";
    public string Description => "Search the web";
    public string IconGlyph => "\ue11a";
    public string ActionKeyword => string.Empty;

    private readonly SpurConfig _config;

    public WebPlugin(SpurConfig config) => _config = config;

    public Task InitAsync(PluginInitContext context, CancellationToken ct = default) => Task.CompletedTask;

    public Task<List<PluginResult>> QueryAsync(Query query, CancellationToken ct)
    {
        if (!_config.IndexWebSearches) return Task.FromResult(new List<PluginResult>());

        var q = NormalizeWebQuery(query.Search);
        if (string.IsNullOrWhiteSpace(q)) return Task.FromResult(new List<PluginResult>());

        return Task.FromResult(new List<PluginResult>
        {
            new()
            {
                Id = $"web:{q}",
                Title = $"Search the web for \"{q}\"",
                Subtitle = "Web",
                Section = Name,
                IconGlyph = "\ue11a",
                IconPath = "/Assets/Icons/search.png",
                Score = 50,
            },
        });
    }

    private static string NormalizeWebQuery(string query)
    {
        var q = query.Trim();
        return q.StartsWith('?') ? q[1..].Trim() : q;
    }
}
