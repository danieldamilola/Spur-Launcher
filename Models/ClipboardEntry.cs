using System.Windows.Media.Imaging;

namespace Volt.Models;

/// <summary>A single clipboard history entry — either text or an image.</summary>
public sealed class ClipboardEntry
{
    /// <summary>Text constructor.</summary>
    public ClipboardEntry(string content)
    {
        Content   = content;
        Timestamp = DateTime.Now;
        IsImage   = false;
        Preview   = content.Length > 60
            ? content[..60].Replace('\n', ' ').Replace('\r', ' ') + "…"
            : content.Replace('\n', ' ').Replace('\r', ' ');
    }

    /// <summary>Image constructor. The BitmapSource must already be frozen.</summary>
    public ClipboardEntry(BitmapSource image)
    {
        Content   = string.Empty;
        Timestamp = DateTime.Now;
        IsImage   = true;
        Image     = image;
        Preview   = $"Image  {image.PixelWidth} × {image.PixelHeight}";
    }

    public string      Content   { get; }
    public DateTime    Timestamp { get; }
    public bool        IsImage   { get; }
    public BitmapSource? Image   { get; }

    /// <summary>Truncated single-line preview for display in results list.</summary>
    public string Preview { get; }

    public string TimeAgo
    {
        get
        {
            var elapsed = DateTime.Now - Timestamp;
            if (elapsed.TotalSeconds < 60) return "just now";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            return $"{(int)elapsed.TotalHours}h ago";
        }
    }
}
