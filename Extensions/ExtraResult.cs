namespace Spur.Extensions;

/// <summary>
/// Structured result returned by IExtra.ExecuteAsync().
/// MainViewModel uses this to update the action panel without knowing
/// the internals of what the extra did.
/// </summary>
public sealed class ExtraResult
{
    public static ExtraResult Empty => new();

    public bool   Success    { get; init; }
    public string Title      { get; init; } = string.Empty;
    public string Detail     { get; init; } = string.Empty;

    /// <summary>If set, this text is automatically copied to the clipboard.</summary>
    public string? CopyText  { get; init; }

    /// <summary>
    /// Which action panel to show (e.g. "calc", "timer", "ai", "color").
    /// Maps to the existing ActiveActionPanel string in MainViewModel.
    /// </summary>
    public string PanelId    { get; init; } = string.Empty;

    /// <summary>Sub-text shown below the main result in the action panel.</summary>
    public string SubText    { get; init; } = string.Empty;
}
