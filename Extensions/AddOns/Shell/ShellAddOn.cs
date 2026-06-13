using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.AddOns.Shell;

public sealed class ShellAddOn : IAddOn
{
    public string Id => "shell";
    public string Name => "Shell";
    public string Description => "Run commands through your configured terminal.";
    public string IconGlyph => "\ue765";
    public string? IconPath => "/Assets/Icons/shell.png";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = ">";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView()
    {
        return new ShellSettingsView { DataContext = Settings };
    }

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var command = subQuery.Trim();
        yield return new SearchResult
        {
            Id = string.IsNullOrWhiteSpace(command) ? "action:shell" : $"shell:{command}",
            Type = ResultType.Action,
            Name = string.IsNullOrWhiteSpace(command) ? "Run Command" : $"Run {command}",
            Subtitle = string.IsNullOrWhiteSpace(command) ? "Type a command to run" : "Press ↵ to execute",
            IconGlyph = IconGlyph,
            IconPath = IconPath,
            ActionId = Id,
            Score = 600,
        };
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
