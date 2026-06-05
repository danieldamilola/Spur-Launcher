using System.Diagnostics;

namespace Spur.Extensions;

/// <summary>
/// Quick Note action. Triggered by "note [text]", e.g. "note buy groceries".
/// Saves a timestamped note to Documents/Spur/notes.txt and opens it.
/// </summary>
public sealed class QuickNoteAction : IAction
{
    public string Id => "note";
    public string Name => "Note";
    public string IconGlyph => "\ue8a5";
    public bool IsGlobal => false;

    private static readonly Regex _trigger = new(
        @"^note\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string _notesDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Spur");
    private static readonly string _notesFile = Path.Combine(_notesDir, "notes.txt");

    // ── Keyword-scoped ────────────────────────────────────────────
    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var text = subQuery.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return new SearchResult
            {
                Id         = "action:note",
                Type       = ResultType.Action,
                Name       = "Quick Note",
                Subtitle   = "Type your note text…",
                IconGlyph  = "\ue8a5",
                ActionId   = Id,
            };
            yield break;
        }
        var preview = text is { Length: > 40 } ? text[..40] + "…" : text;
        yield return new SearchResult
        {
            Id         = "action:note",
            Type       = ResultType.Action,
            Name       = "Save Quick Note",
            Subtitle   = $"\"{preview}\"",
            IconGlyph  = "\ue8a5",
            ActionId   = Id,
        };
    }

    // ── Legacy (global) ───────────────────────────────────────────
    public bool CanHandle(string query)
        => !string.IsNullOrWhiteSpace(query) && _trigger.IsMatch(query.Trim());

    public SearchResult BuildResult(string query)
    {
        var text = ExtractText(query);
        var preview = text is { Length: > 40 } ? text[..40] + "…" : text;
        return new SearchResult
        {
            Id         = "action:note",
            Type       = ResultType.Action,
            Name       = "Save Quick Note",
            Subtitle   = string.IsNullOrWhiteSpace(preview) ? "Press ↵ to save" : $"\"{preview}\"",
            IconGlyph = "\ue8a5",
            ActionId   = Id,
        };
    }

    public static string? ExtractText(string query)
    {
        var m = _trigger.Match(query.Trim());
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    public static void Execute(string query)
    {
        var text = ExtractText(query);
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            Directory.CreateDirectory(_notesDir);
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}{Environment.NewLine}";
            File.AppendAllText(_notesFile, entry);

            Process.Start(new ProcessStartInfo(_notesFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[QuickNote] Failed: {ex.Message}");
        }
    }
}
