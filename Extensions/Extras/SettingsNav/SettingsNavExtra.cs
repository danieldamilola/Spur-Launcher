using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.Extras.SettingsNav;

public sealed class SettingsNavExtra : IExtra
{
    public string Id => "settings";
    public string Name => "Settings";
    public string Description => "Open Spur settings.";
    public string IconGlyph => "\ue713";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "settings";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView() => null;

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        yield return new SearchResult
        {
            Id = "action:settings",
            Type = ResultType.Action,
            Name = "Settings",
            Subtitle = "Configure Spur",
            IconGlyph = IconGlyph,
            ActionId = Id,
        };
    }

    public bool CanHandle(string query) => false;
    public SearchResult BuildResult(string query) => new();

    public Task<ExtraResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        return Task.FromResult(new ExtraResult
        {
            Success = true,
            Title = Name,
            Detail = "Opened settings",
            PanelId = Id
        });
    }
}
