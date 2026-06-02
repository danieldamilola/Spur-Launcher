namespace Arc.Models;

/// <summary>Layout constants from ux.md / design.md.</summary>
public static class LauncherLayout
{
    /// <summary>Bar width — fixed in empty and hover (Spotlight-style circles reveal inside).</summary>
    public const double BarWidth = 640;

    public const double WidthCompact  = BarWidth;
    public const double WidthExpanded = BarWidth;

    /// <summary>Space for three 44px category circles + gaps.</summary>
    public const double CategoryZoneWidth = 156;

    public const double BarHeight = 56;
    public const double FooterHeight = 36;
    public const double ScopeRowHeight = 32;
    public const double MaxContentHeight = 424;
    public const double MaxShelfRows = 8;
    public const double ShelfRowHeight = 44;
    public const double MaxShelfListHeight = MaxShelfRows * ShelfRowHeight;
}
