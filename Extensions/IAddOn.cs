using System.Windows;
using Spur.Models;

namespace Spur.Extensions;

/// <summary>
/// A Spur add-on (formerly "action"). Built-in and community add-ons both implement this.
/// Each add-on is self-contained: it owns its search results, settings model, settings UI,
/// and execution logic. No changes to SearchEngineService or MainViewModel are needed
/// when adding a new add-on — just register it in AddOnRegistry.
/// </summary>
public interface IAddOn
{
    // ── Identity ─────────────────────────────────────────────────────
    string Id          { get; }   // e.g. "calc", "timer", "pw"
    string Name        { get; }   // e.g. "Calculator", shown in UI
    string Description { get; }   // shown in the Add-ons store card
    string IconGlyph   { get; }   // Segoe MDL2 / custom glyph (fallback)
    /// <summary>Resource path to a colored PNG icon (e.g. "/Assets/Icons/calculator.png"). Null = use IconGlyph fallback.</summary>
    string? IconPath    { get; }
    string Author      { get; }   // "Built-in" for shipped add-ons
    string Version     { get; }
    bool   IsBuiltIn   { get; }   // false = community / downloaded

    // ── Behaviour ────────────────────────────────────────────────────
    bool   IsEnabled   { get; set; }
    string Keyword     { get; set; }  // e.g. "timer", ">" for shell
    bool   IsGlobal    { get; }       // true = activates inline without keyword (Calc only)

    // ── Settings ─────────────────────────────────────────────────────
    /// <summary>
    /// Per-add-on settings POCO. Cast to the concrete type (e.g. TimerSettings) when needed.
    /// Null for add-ons with no configurable options (e.g. SettingsAction).
    /// </summary>
    object? Settings { get; set; }

    /// <summary>
    /// Returns the add-on's own settings UI panel, or null if the add-on has no settings.
    /// Returned element is embedded in the Add-ons tab accordion when expanded.
    /// </summary>
    FrameworkElement? CreateSettingsView();

    // ── Search ───────────────────────────────────────────────────────
    /// <summary>True if this global add-on should handle the current query inline.</summary>
    bool CanHandle(string query);

    /// <summary>Builds the inline result row for a global add-on match.</summary>
    SearchResult BuildResult(string query);

    /// <summary>
    /// For keyword-scoped add-ons: returns results for the sub-query (after the keyword).
    /// When subQuery is empty, returns all available options (presets, commands, etc.).
    /// </summary>
    IEnumerable<SearchResult> GetResults(string subQuery);

    // ── Execution ────────────────────────────────────────────────────
    /// <summary>
    /// Executes the add-on's action for the given input string.
    /// Returns a structured result so MainViewModel can show the action panel
    /// without knowing the details of what the add-on did.
    /// </summary>
    Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default);
}
