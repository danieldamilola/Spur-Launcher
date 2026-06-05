namespace Spur.Extensions;

/// <summary>
/// A built-in action. Global actions (Calculator) match in any search.
/// Keyword actions (System, Timer, etc.) only activate when their keyword
/// is typed followed by a space, e.g. "sys shut".
/// </summary>
public interface IAction
{
    /// <summary>Unique identifier, e.g. "calc", "timer", "color".</summary>
    string Id { get; }

    /// <summary>Human-readable name shown in the scope bar, e.g. "System".</summary>
    string Name { get; }

    /// <summary>Lucide icon glyph or Unicode char for the scope bar icon.</summary>
    string IconGlyph { get; }

    /// <summary>
    /// True if this action also activates globally without a keyword.
    /// Only Calculator should return true. All others return false.
    /// </summary>
    bool IsGlobal { get; }

    /// <summary>
    /// For keyword-scoped actions: returns results for the given sub-query
    /// (the part after the keyword). When subQuery is empty, returns all
    /// available options (e.g. all system commands, all timer presets).
    /// </summary>
    IEnumerable<SearchResult> GetResults(string subQuery);

    // -- Legacy (for global actions) --
    /// <summary>Returns true if this global action should handle the query.</summary>
    bool CanHandle(string query);
    /// <summary>Builds the result row for a global action match.</summary>
    SearchResult BuildResult(string query);
}
