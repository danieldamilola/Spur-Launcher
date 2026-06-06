using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace Spur.Extensions;

/// <summary>
/// Screenshot action. Triggered by "ss" or "screenshot".
/// Captures the primary screen and saves to Desktop.
/// </summary>
public sealed class ScreenshotAction : IAction
{
    public string Id => "screenshot";
    public string Name => "Screenshot";
    public string IconGlyph => "\ue722";
    public bool IsGlobal => false;

    // ── Keyword-scoped ────────────────────────────────────────────
    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        yield return BuildResult("screenshot");
    }

    // ── Legacy (global) ───────────────────────────────────────────
    public bool CanHandle(string query)
        => string.Equals(query.Trim(), "screenshot", StringComparison.OrdinalIgnoreCase)
        || string.Equals(query.Trim(), "screen", StringComparison.OrdinalIgnoreCase)
        || string.Equals(query.Trim(), "ss", StringComparison.OrdinalIgnoreCase);

    public SearchResult BuildResult(string query) => new()
    {
        Id         = "action:screenshot",
        Type       = ResultType.Action,
        Name       = "Take Screenshot",
        Subtitle   = "Press ↵ to capture the screen",
        IconGlyph = "\ue722",
        ActionId   = Id,
    };

    public static string? Execute()
    {
        try
        {
            var bounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
            using var bmp = new Bitmap(bounds.Width, bounds.Height);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var file = Path.Combine(desktop, $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");
            bmp.Save(file, ImageFormat.Png);

            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            return file;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Screenshot] Failed: {ex.Message}");
            return null;
        }
    }
}
