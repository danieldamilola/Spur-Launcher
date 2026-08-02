using Spur.Models;
using Spur.Plugin;
using Spur.Services;
using System.Windows.Media.Imaging;
using Query = Spur.Plugin.Query;

namespace Spur.Plugins;

public sealed class ClipboardPlugin : IPlugin
{
    public string Id => "clipboard";
    public string Name => "Clipboard";
    public string Description => "Clipboard history search";
    public string IconGlyph => "clipboard";
    public string ActionKeyword => string.Empty;

    private readonly IClipboardService _clipboard;
    private readonly SpurConfig _config;

    public ClipboardPlugin(IClipboardService clipboard, SpurConfig config)
    {
        _clipboard = clipboard;
        _config = config;
    }

    public Task InitAsync(PluginInitContext context, CancellationToken ct = default) => Task.CompletedTask;

    public Task<List<PluginResult>> QueryAsync(Query query, CancellationToken ct)
    {
        if (!_config.ClipboardEnabled || !_config.IndexClipboard)
            return Task.FromResult(new List<PluginResult>());

        var q = query.Search;
        const int limit = 3;
        var clips = new List<PluginResult>(limit);
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
            if (!string.IsNullOrEmpty(q) && PluginHelper.MatchScore(q, c.Preview, _config, sr) < 0) continue;

            clips.Add(new PluginResult
            {
                Id = sr.Id,
                Title = sr.Name,
                Subtitle = sr.Subtitle,
                Section = Name,
                IconGlyph = sr.IconGlyph,
                IconPath = sr.IconPath ?? string.Empty,
                Source = sr,
            });
        }

        return Task.FromResult(clips);
    }
}
