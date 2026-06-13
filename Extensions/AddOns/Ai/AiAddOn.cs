using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.AddOns.Ai;

public sealed class AiAddOn : IAddOn
{
    public string Id => "ai";
    public string Name => "AI Assistant";
    public string Description => "Ask the configured AI provider.";
    public string IconGlyph => "\ue270";
    public string? IconPath => "/Assets/Icons/find.png";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "ai";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView() => new AiSettingsView();

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        // AI UI is being redesigned — no search results surfaced for now.
        yield break;
    }

    public bool CanHandle(string query) => false;
    public SearchResult BuildResult(string query) => new();

    public Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        return Task.FromResult(new AddOnResult
        {
            Success = true,
            Title = Name,
            Detail = input,
            PanelId = Id
        });
    }
}
