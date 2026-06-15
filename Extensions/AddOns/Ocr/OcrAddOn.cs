using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Spur.Models;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace Spur.Extensions.AddOns.Ocr;

/// <summary>
/// OCR add-on — extracts text from clipboard images using Windows.Media.Ocr.OcrEngine.
/// Requires net9.0-windows10.0.17763.0 target framework.
/// Triggered via the "ocr" keyword.
/// </summary>
public sealed class OcrAddOn : IAddOn
{
    public string Id => "ocr";
    public string Name => "OCR";
    public string Description => "Extract text from clipboard images.";
    public string IconGlyph => "\uE8FE";
    public string? IconPath => null;
    public string Author => "Built-in";
    public string Version => "1.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "ocr";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView() => null;

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        bool hasImage = false;
        try
        {
            hasImage = System.Windows.Clipboard.ContainsImage();
        }
        catch { /* clipboard may be locked */ }

        if (hasImage)
        {
            yield return new SearchResult
            {
                Id        = "action:ocr:extract",
                Type      = ResultType.Action,
                Name      = "Extract Text from Clipboard Image",
                Subtitle  = "Press Enter to run OCR on the clipboard image",
                IconGlyph = IconGlyph,
                ActionId  = Id,
            };
        }
        else
        {
            yield return new SearchResult
            {
                Id        = "action:ocr:noimage",
                Type      = ResultType.Action,
                Name      = "OCR — No image in clipboard",
                Subtitle  = "Copy an image to clipboard first (e.g. Win+Shift+S)",
                IconGlyph = IconGlyph,
                ActionId  = Id,
            };
        }
    }

    public bool CanHandle(string query) => false;
    public SearchResult BuildResult(string query) => new();

    public async Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        try
        {
            // Get the image from the clipboard on the UI thread
            BitmapSource? bitmapSource = null;

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (System.Windows.Clipboard.ContainsImage())
                    bitmapSource = System.Windows.Clipboard.GetImage();
            });

            if (bitmapSource is null)
            {
                return new AddOnResult
                {
                    Success = false,
                    Title   = "No image in clipboard",
                    Detail  = "Copy an image to clipboard first.",
                };
            }

            // Convert WPF BitmapSource to a byte array (PNG)
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));

            using var memoryStream = new MemoryStream();
            encoder.Save(memoryStream);
            memoryStream.Position = 0;

            // Convert to Windows.Graphics.Imaging.SoftwareBitmap
            var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(
                memoryStream.AsRandomAccessStream());
            var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
                Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied);

            // Run OCR
            var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            if (ocrEngine is null)
            {
                return new AddOnResult
                {
                    Success = false,
                    Title   = "OCR engine unavailable",
                    Detail  = "No OCR language packs installed.",
                };
            }

            var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
            var extractedText = ocrResult.Text;

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return new AddOnResult
                {
                    Success = true,
                    Title   = "No text found",
                    Detail  = "The OCR engine could not detect any text in the image.",
                };
            }

            return new AddOnResult
            {
                Success  = true,
                Title    = "Text Extracted",
                Detail   = extractedText,
                CopyText = extractedText,
                SubText  = $"Extracted {extractedText.Split('\n').Length} line(s)",
                PanelId  = Id,
            };
        }
        catch (Exception ex)
        {
            return new AddOnResult
            {
                Success = false,
                Title   = "OCR failed",
                Detail  = ex.Message,
            };
        }
    }
}
