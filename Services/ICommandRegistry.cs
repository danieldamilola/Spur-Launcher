namespace Spur.Services;

/// <summary>
/// A registered command entry for the command palette.
/// </summary>
public sealed record CommandPaletteEntry
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Description { get; init; }
    public required string IconGlyph { get; init; }
    public string? IconPath { get; init; }
    public required Action Execute { get; init; }
}

/// <summary>
/// Registry of actions exposed via Ctrl+Shift+P command palette.
/// Register at startup; filter/search at runtime.
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// All registered commands (alphabetical by label).
    /// </summary>
    IReadOnlyList<CommandPaletteEntry> All { get; }

    /// <summary>
    /// Register a single command entry. Duplicate Ids are silently ignored.
    /// </summary>
    void Register(CommandPaletteEntry entry);

    /// <summary>
    /// Search entries by label/description substring (case-insensitive).
    /// </summary>
    IEnumerable<CommandPaletteEntry> Search(string filter);

    /// <summary>
    /// Find a specific entry by Id.
    /// </summary>
    CommandPaletteEntry? Find(string id);
}
