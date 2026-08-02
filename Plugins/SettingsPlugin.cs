using Spur.Models;
using Spur.Plugin;
using Query = Spur.Plugin.Query;

namespace Spur.Plugins;

public sealed class SettingsPlugin : IPlugin
{
    public string Id => "settings";
    public string Name => "Settings";
    public string Description => "Windows Settings pages";
    public string IconGlyph => "\uE713";
    public string ActionKeyword => string.Empty;

    private readonly SpurConfig _config;

    public SettingsPlugin(SpurConfig config) => _config = config;

    public Task InitAsync(PluginInitContext context, CancellationToken ct = default) => Task.CompletedTask;

    public Task<List<PluginResult>> QueryAsync(Query query, CancellationToken ct)
    {
        var q = query.Search;
        if (!_config.IndexWindowsSettings || string.IsNullOrEmpty(q))
            return Task.FromResult(new List<PluginResult>());

        var matches = new List<PluginResult>();
        foreach (var s in SpurConstants.WindowsSettings)
        {
            var sr = new SearchResult
            {
                Id = s.Id, Type = s.Type, Name = s.Name, Subtitle = s.Subtitle,
                IconPath = s.IconPath, IconGlyph = s.IconGlyph,
                FrequencyScore = s.FrequencyScore, IsPinned = s.IsPinned,
                ExePath = s.ExePath, LnkPath = s.LnkPath,
                FilePath = s.FilePath, FileExtension = s.FileExtension,
                IsDirectory = s.IsDirectory, ActionId = s.ActionId,
            };
            var sc = PluginHelper.MatchScore(q, s.Name, _config, sr);
            if (sc < 0) continue;
            sr.Score = sc;
            matches.Add(new PluginResult
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
        matches.Sort((x, y) => y.Score.CompareTo(x.Score));
        if (matches.Count > 4) matches.RemoveRange(4, matches.Count - 4);
        return Task.FromResult(matches);
    }
}
