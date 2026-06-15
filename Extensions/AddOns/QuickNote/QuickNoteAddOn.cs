using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.AddOns.QuickNote;

public sealed class QuickNoteAddOn : IAddOn
{
    public string Id => "note";
    public string Name => "Quick Note";
    public string Description => "Save quick notes instantly.";
    public string IconGlyph => "\ue70b";
    public string? IconPath => "/Assets/Icons/note.png";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "note";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView()
    {
        return new QuickNoteSettingsView { DataContext = Settings };
    }

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var note = subQuery.Trim();
        if (string.IsNullOrWhiteSpace(note))
        {
            yield return new SearchResult
            {
                Id         = "action:note:empty",
                Type       = ResultType.Action,
                Name       = "New Quick Note",
                Subtitle   = "Type your note...",
                IconGlyph  = IconGlyph,
                IconPath   = IconPath,
                ActionId   = Id,
            };
            yield break;
        }

        yield return new SearchResult
        {
            Id         = $"action:note:{note.GetHashCode()}",
            Type       = ResultType.Action,
            Name       = "Save Note",
            Subtitle   = $"Press ↵ to save to SpurNotes.txt: {note}",
            IconGlyph  = IconGlyph,
            IconPath   = IconPath,
            ActionId   = Id,
        };
    }

    public bool CanHandle(string query) => false;

    public SearchResult BuildResult(string query) => new();

    public Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var note = input.Trim();
        if (string.IsNullOrWhiteSpace(note))
            return Task.FromResult(new AddOnResult { Success = false });

        var settings = (Settings as QuickNoteSettings) ?? new QuickNoteSettings();
        
        try
        {
            var folder = settings.SaveFolder;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            var file = Path.Combine(folder, "SpurNotes.txt");
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var entry = $"--- {timestamp} ---\r\n{note}\r\n\r\n";

            File.AppendAllText(file, entry);

            return Task.FromResult(new AddOnResult
            {
                Success = true,
                Title = Name,
                Detail = file,
                PanelId = Id,
                SubText = "Saved to SpurNotes.txt"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AddOnResult
            {
                Success = false,
                Title = Name,
                Detail = $"Error: {ex.Message}",
                PanelId = Id
            });
        }
    }
}
