namespace Spur.Models;

/// <summary>
/// Represents one of the pinned category shortcut buttons shown below the search bar.
/// Previously a nested class inside MainViewModel.
/// </summary>
public sealed class PinnedCategoryItem
{
    public string Id        { get; set; } = string.Empty;
    public string Label     { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = string.Empty;
}
