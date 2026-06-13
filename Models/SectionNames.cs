namespace Spur.Models;

/// <summary>
/// Canonical string constants for result section headers and scope filter IDs.
/// Replaces magic string literals that were duplicated in SectionMatchesScope,
/// SectionToScopeId, SectionToScopeLabel, and BuildWebSearch/BuildActionCatalog.
/// </summary>
public static class SectionNames
{
    // ── Section header titles (shown in the result list UI) ────────
    public const string Applications = "Applications";
    public const string Files        = "Files";
    public const string Clipboard    = "Clipboard";
    public const string Commands     = "Commands";
    public const string Settings     = "Settings";
    public const string Web          = "Web";

    // ── Scope filter IDs (used by ActiveScopeId / ScopeFilterItem) ─
    public const string ScopeAll       = "all";
    public const string ScopeApps      = "apps";
    public const string ScopeFiles     = "files";
    public const string ScopeClipboard = "clipboard";
    public const string ScopeCommands  = "commands";
    public const string ScopeWeb       = "web";

    // ── Category IDs (used by ActiveCategory) ─────────────────────
    public const string CategoryApps      = "apps";
    public const string CategoryFiles     = "files";
    public const string CategoryClipboard = "clipboard";
    public const string CategoryActions   = "actions";
}
