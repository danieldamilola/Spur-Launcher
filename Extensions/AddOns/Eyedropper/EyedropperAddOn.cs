using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.AddOns.Eyedropper;

/// <summary>
/// Color Eyedropper add-on — pick a color from anywhere on screen.
/// Uses the Win32 GetDC/GetPixel API to sample the pixel under the cursor.
/// Triggered via the "pick" keyword.
/// </summary>
public sealed class EyedropperAddOn : IAddOn
{
    public string Id => "eyedropper";
    public string Name => "Eyedropper";
    public string Description => "Pick a color from anywhere on screen.";
    public string IconGlyph => "\uEF3C";
    public string? IconPath => null;
    public string Author => "Built-in";
    public string Version => "1.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "pick";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView() => null;

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var text = subQuery.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            yield return new SearchResult
            {
                Id        = "action:eyedropper:pick",
                Type      = ResultType.Action,
                Name      = "Pick Color from Screen",
                Subtitle  = "Press Enter to sample the pixel under your cursor",
                IconGlyph = IconGlyph,
                ActionId  = Id,
            };
            yield break;
        }

        // Allow "pick" as subquery to also trigger
        if (text.Equals("color", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("screen", StringComparison.OrdinalIgnoreCase))
        {
            yield return new SearchResult
            {
                Id        = "action:eyedropper:pick",
                Type      = ResultType.Action,
                Name      = "Pick Color from Screen",
                Subtitle  = "Press Enter to sample the pixel under your cursor",
                IconGlyph = IconGlyph,
                ActionId  = Id,
            };
        }
    }

    public bool CanHandle(string query) => false;
    public SearchResult BuildResult(string query) => new();

    public Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<AddOnResult>();

        // Must run on UI thread since we're showing a Window
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                var overlay = new Views.EyedropperOverlay();
                overlay.Closed += (_, _) =>
                {
                    if (!overlay.WasCaptured)
                    {
                        tcs.TrySetResult(new AddOnResult
                        {
                            Success = false,
                            Title   = "Eyedropper cancelled",
                            Detail  = "Color picking was cancelled.",
                        });
                        return;
                    }

                    int r = overlay.CapturedR;
                    int g = overlay.CapturedG;
                    int b = overlay.CapturedB;
                    var hex = overlay.CapturedHex!;
                    var rgb = $"rgb({r}, {g}, {b})";

                    // Compute HSL
                    double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
                    double max = Math.Max(rf, Math.Max(gf, bf));
                    double min = Math.Min(rf, Math.Min(gf, bf));
                    double h = 0, s = 0, l = (max + min) / 2.0;

                    if (max != min)
                    {
                        double d = max - min;
                        s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
                        if (max == rf) h = (gf - bf) / d + (gf < bf ? 6 : 0);
                        else if (max == gf) h = (bf - rf) / d + 2;
                        else if (max == bf) h = (rf - gf) / d + 4;
                        h /= 6.0;
                    }

                    var hsl = $"hsl({Math.Round(h * 360)}, {Math.Round(s * 100)}%, {Math.Round(l * 100)}%)";

                    // Determine output format based on settings
                    var settings = Settings as EyedropperSettings;
                    var format = settings?.DefaultFormat?.ToLowerInvariant() ?? "hex";
                    var copyText = format switch
                    {
                        "rgb" => rgb,
                        "hsl" => hsl,
                        _ => hex,
                    };

                    tcs.TrySetResult(new AddOnResult
                    {
                        Success  = true,
                        Title    = $"Picked: {hex}",
                        Detail   = $"{hex}  ·  {rgb}  ·  {hsl}",
                        CopyText = copyText,
                        SubText  = $"Copied {copyText}",
                        PanelId  = Id,
                    });
                };

                overlay.Show();
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(new AddOnResult
                {
                    Success = false,
                    Title   = "Eyedropper failed",
                    Detail  = ex.Message,
                });
            }
        });

        return tcs.Task;
    }
}
