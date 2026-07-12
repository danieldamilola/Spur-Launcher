using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.AddOns.FilesCategory;

public sealed class FilesCategoryAddOn : IAddOn
{
    public string Id => "files";
    public string Name => "Files";
    public string Description => "Search your files and folders.";
    public string IconGlyph => "\uE8B7";
    public string? IconPath => null;
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "";
    public bool IsGlobal => true;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView() => null;

    public IEnumerable<SearchResult> GetResults(string subQuery) => Array.Empty<SearchResult>();
    public bool CanHandle(string query) => false;
    public SearchResult BuildResult(string query) => new();

    public Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        return Task.FromResult(new AddOnResult { Success = true, Title = Name, Detail = input, PanelId = Id });
    }
}
