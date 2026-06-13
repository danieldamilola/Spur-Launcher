namespace Spur.Models;

/// <summary>A single sidebar section in the settings UI.</summary>
public record SettingsSection(string Name, string IconGlyph, string? IconPath = null);
