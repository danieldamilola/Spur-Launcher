using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Spur.Models;

namespace Spur.Extensions.AddOns.Screenshot;

public sealed class ScreenshotAddOn : IAddOn
{
    public string Id => "ss";
    public string Name => "Screenshot";
    public string Description => "Capture and save screenshots.";
    public string IconGlyph => "\ue722";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "ss";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView()
    {
        return new ScreenshotSettingsView { DataContext = Settings };
    }

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        yield return new SearchResult
        {
            Id         = "action:screenshot",
            Type       = ResultType.Action,
            Name       = "Capture Screenshot",
            Subtitle   = "Press ↵ to capture entire screen",
            IconGlyph  = IconGlyph,
            ActionId   = Id,
        };
    }

    public bool CanHandle(string query) => false;

    public SearchResult BuildResult(string query) => new();

    public Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var settings = (Settings as ScreenshotSettings) ?? new ScreenshotSettings();
        
        try
        {
            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            using var bmp = new Bitmap(bounds.Width, bounds.Height);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(bounds.Location, global::System.Drawing.Point.Empty, bounds.Size);

            var folder = settings.SaveFolder;
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var formatStr = settings.SaveFormat.ToLowerInvariant() == "jpeg" ? "jpg" : settings.SaveFormat.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(formatStr)) formatStr = "png";

            var file = Path.Combine(folder, $"Screenshot_{timestamp}.{formatStr}");

            global::System.Drawing.Imaging.ImageFormat format = settings.SaveFormat.ToLowerInvariant() switch
            {
                "jpg" or "jpeg" => global::System.Drawing.Imaging.ImageFormat.Jpeg,
                "bmp"           => global::System.Drawing.Imaging.ImageFormat.Bmp,
                "gif"           => global::System.Drawing.Imaging.ImageFormat.Gif,
                _               => global::System.Drawing.Imaging.ImageFormat.Png
            };

            bmp.Save(file, format);
            
            return Task.FromResult(new AddOnResult
            {
                Success = true,
                Title = Name,
                Detail = file,
                PanelId = Id,
                SubText = $"Saved to {file}"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AddOnResult
            {
                Success = false,
                Title = Name,
                Detail = $"Error: {ex.Message}",
                PanelId = Id
            });
        }
    }
}
