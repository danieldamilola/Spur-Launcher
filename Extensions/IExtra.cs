using System.Windows;
using Spur.Models;

namespace Spur.Extensions;

/// <summary>
/// A Spur extra (formerly "action"). Built-in and community extras both implement this.
/// Each extra is self-contained: it owns its search results, settings model, settings UI,
/// and execution logic. No changes to SearchEngineService or MainViewModel are needed
/// when adding a new extra — just register it in ExtrasRegistry.
/// </summary>
public interface IExtra
{
    // ── Identity ─────────────────────────────────────────────────────
    string Id          { get; }   // e.g. "calc", "timer", "pw"
    string Name        { get; }   // e.g. "Calculator", shown in UI
    string Description { get; }   // shown in the Extras store card
    string IconGlyph   { get; }   // Segoe MDL2 / custom glyph
    string Author      { get; }   // "Built-in" for shipped extras
    string Version     { get; }
    bool   IsBuiltIn   { get; }   // false = community / downloaded

    // ── Behaviour ────────────────────────────────────────────────────
    bool   IsEnabled   { get; set; }
    string Keyword     { get; set; }  // e.g. "timer", ">" for shell
    bool   IsGlobal    { get; }       // true = activates inline without keyword (Calc only)

    // ── Settings ─────────────────────────────────────────────────────
    /// <summary>
    /// Per-extra settings POCO. Cast to the concrete type (e.g. TimerSettings) when needed.
    /// Null for extras with no configurable options (e.g. SettingsAction).
    /// </summary>
    object? Settings { get; set; }

    /// <summary>
    /// Returns the extra's own settings UI panel, or null if the extra has no settings.
    /// Returned element is embedded in the Extras tab accordion when expanded.
    /// </summary>
    FrameworkElement? CreateSettingsView();

    // ── Search ───────────────────────────────────────────────────────
    /// <summary>True if this global extra should handle the current query inline.</summary>
    bool CanHandle(string query);

    /// <summary>Builds the inline result row for a global extra match.</summary>
    SearchResult BuildResult(string query);

    /// <summary>
    /// For keyword-scoped extras: returns results for the sub-query (after the keyword).
    /// When subQuery is empty, returns all available options (presets, commands, etc.).
    /// </summary>
    IEnumerable<SearchResult> GetResults(string subQuery);

    // ── Execution ────────────────────────────────────────────────────
    /// <summary>
    /// Executes the extra's action for the given input string.
    /// Returns a structured result so MainViewModel can show the action panel
    /// without knowing the details of what the extra did.
    /// </summary>
    Task<ExtraResult> ExecuteAsync(string input, CancellationToken ct = default);
}
