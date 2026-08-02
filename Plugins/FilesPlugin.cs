using Spur.Plugin;
using Spur.Services;
using Query = Spur.Plugin.Query;

namespace Spur.Plugins;

public sealed class FilesPlugin : IPlugin
{
    public string Id => "files";
    public string Name => "Files";
    public string Description => "File search via Everything SDK and Windows Search";
    public string IconGlyph => "\uE8B7";
    public string ActionKeyword => string.Empty;

    private readonly IFileSearchService _files;
    private readonly SpurConfig _config;

    public FilesPlugin(IFileSearchService files, SpurConfig config)
    {
        _files = files;
        _config = config;
    }

    public Task InitAsync(PluginInitContext context, CancellationToken ct = default) => Task.CompletedTask;

    public async Task<List<PluginResult>> QueryAsync(Query query, CancellationToken ct)
    {
        if (!_config.FileSearchEnabled) return [];

        int limit = _config.ResultsCount;
        var items = string.IsNullOrEmpty(query.Search)
            ? await _files.BrowseRecentAsync(limit)
            : await _files.SearchAsync(query.Search, limit, ct);

        return items.Select(item => new PluginResult
        {
            Id = item.Id,
            Title = item.Name,
            Subtitle = item.Subtitle,
            Section = Name,
            IconGlyph = item.IconGlyph,
            IconPath = item.IconPath ?? string.Empty,
            Score = (int)item.Score,
            Source = item,
        }).ToList();
    }
}
