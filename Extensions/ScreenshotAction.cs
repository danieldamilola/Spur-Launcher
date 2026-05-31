using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace Arc.Extensions;

/// <summary>
/// Screenshot action. Triggered by typing exactly "screenshot" or "screen".
/// Captures the primary screen and saves to Desktop.
/// </summary>
public sealed class ScreenshotAction : IAction
{
    public string Id => "screenshot";

    public bool CanHandle(string query)
        => string.Equals(query.Trim(), "screenshot", StringComparison.OrdinalIgnoreCase)
        || string.Equals(query.Trim(), "screen", StringComparison.OrdinalIgnoreCase);

    public SearchResult BuildResult(string query) => new()
    {
        Id         = "action:screenshot",
        Type       = ResultType.Action,
        Name       = "Take Screenshot",
        Subtitle   = "Press ↵ to capture the screen",
        LucideIcon = "camera",
        ActionId   = Id,
    };

    public static void Execute()
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
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Screenshot] Failed: {ex.Message}");
        }
    }
}
