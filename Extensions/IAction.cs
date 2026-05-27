namespace Volt.Extensions;

/// <summary>
/// A built-in action that activates automatically when the query matches a pattern.
/// Actions produce a single SearchResult row and power the preview panel.
/// </summary>
public interface IAction
{
    /// <summary>Unique identifier, e.g. "calc", "timer", "color", "ip", "ai".</summary>
    string Id { get; }

    /// <summary>Returns true if this action should handle the given query.</summary>
    bool CanHandle(string query);

    /// <summary>Builds the result row shown in the results list.</summary>
    SearchResult BuildResult(string query);
}
