namespace Arc.Models;

/// <summary>
/// Canonical paths for Arc launcher brand icons under <c>Icons/</c>.
/// Files are embedded as WPF resources (pack URIs) and used for exe, tray, and installers.
/// </summary>
public static class AppIcons
{
    public const string Folder = "Icons";

    public const string Size16  = "arc-launcher-16x16.ico";
    public const string Size32  = "arc-launcher-32x32.ico";
    public const string Size64  = "arc-launcher-64x64.ico";
    public const string Size128 = "arc-launcher-128x128.ico";
    public const string Size256 = "arc-launcher-256x256.ico";
    public const string Size512 = "arc-launcher-512x512.ico";
    public const string Size1024 = "arc-launcher-1024x1024.ico";

    /// <summary>Embedded in the .exe — compile-time <see cref="ApplicationIcon"/>.</summary>
    public const string Application = Size256;

    /// <summary>System tray notification area (16×16 logical).</summary>
    public const string Tray = Size16;

    /// <summary>Velopack / installer packaging.</summary>
    public const string Installer = Size256;

    /// <summary>Legacy path kept for scripts that still reference <c>Assets\arc.ico</c>.</summary>
    public const string AssetsLegacy = "Assets/arc.ico";

    public static Uri PackUri(string fileName)
        => new($"pack://application:,,,/{Folder}/{fileName}", UriKind.Absolute);

    public static string RelativePath(string fileName)
        => $"{Folder}/{fileName}";
}
