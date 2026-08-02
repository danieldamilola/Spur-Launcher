using System.Collections.Concurrent;
using Spur.Plugin;

namespace Spur.Core;

public class PluginManager
{
    private readonly List<PluginPair> _plugins = [];
    private readonly ConcurrentDictionary<string, List<PluginPair>> _keywordMap = new();
    private readonly List<PluginPair> _globalPlugins = [];

    private bool _initialized;

    public PluginManager(IEnumerable<IPlugin> plugins)
    {
        foreach (var plugin in plugins)
            Register(plugin);
    }

    public void Register(IPlugin plugin)
    {
        var metadata = new PluginMetadata
        {
            Id = plugin.Id,
            Name = plugin.Name,
            Description = plugin.Description,
        };
        var pair = new PluginPair(plugin, metadata);
        _plugins.Add(pair);

        var keyword = plugin.ActionKeyword;
        if (string.IsNullOrEmpty(keyword))
            _globalPlugins.Add(pair);
        else
            _keywordMap.AddOrUpdate(keyword, _ => [pair], (_, list) => { list.Add(pair); return list; });
    }

    public IEnumerable<PluginPair> AllPlugins => _plugins;

    public async Task InitializeAllAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        foreach (var pair in _plugins)
            await pair.Plugin.InitAsync(new PluginInitContext { Metadata = pair.Metadata }, ct);
        _initialized = true;
    }

    public List<PluginPair> ResolvePlugins(string? actionKeyword)
    {
        if (string.IsNullOrEmpty(actionKeyword))
            return _globalPlugins;

        if (_keywordMap.TryGetValue(actionKeyword, out var keywordPlugins))
            return keywordPlugins;

        return _globalPlugins;
    }

    public async Task<List<(PluginPair Source, List<PluginResult> Results)>> QueryAsync(
        string query, string? actionKeyword, CancellationToken ct)
    {
        var pairs = ResolvePlugins(actionKeyword);
        if (pairs.Count == 0) return [];

        var queryObj = new Query
        {
            RawQuery = query,
            Search = query,
            ActionKeyword = actionKeyword ?? string.Empty,
        };

        var tasks = pairs.Select(pair => QueryPluginAsync(pair, queryObj, ct));
        var results = await Task.WhenAll(tasks);

        return results.Where(r => r.Results.Count > 0).ToList();
    }

    private static async Task<(PluginPair Source, List<PluginResult> Results)> QueryPluginAsync(
        PluginPair pair, Query queryObj, CancellationToken ct)
    {
        try
        {
            var results = await pair.Plugin.QueryAsync(queryObj, ct);
            return (pair, results ?? []);
        }
        catch (OperationCanceledException)
        {
            return (pair, []);
        }
    }
}
