using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.Extras.Ai;

public sealed class AiExtra : IExtra
{
    public string Id => "ai";
    public string Name => "AI Assistant";
    public string Description => "Ask the configured AI provider.";
    public string IconGlyph => "\ue270";
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
        if (string.IsNullOrWhiteSpace(subQuery))
        {
            yield return new SearchResult
            {
                Id = "action:ai",
                Type = ResultType.Action,
                Name = "Ask AI",
                Subtitle = "Type your question…",
                IconGlyph = IconGlyph,
                ActionId = Id,
            };
            yield break;
        }

        yield return new SearchResult
        {
            Id = $"ai:{subQuery}",
            Type = ResultType.Action,
            Name = $"Ask: {subQuery}",
            Subtitle = "Press ↵ to send",
            IconGlyph = IconGlyph,
            ActionId = Id,
            Score = 600,
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
            Detail = input,
            PanelId = Id
        });
    }
}
