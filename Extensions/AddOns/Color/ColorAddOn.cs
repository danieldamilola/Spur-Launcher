using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.AddOns.Color;

public sealed class ColorAddOn : IAddOn
{
    public string Id => "color";
    public string Name => "Color";
    public string Description => "Hex/RGB/HSL color conversion and preview.";
    public string IconGlyph => "\ue790";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "color";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView() => null;

    private static readonly Regex _hexRegex = new(@"^#?([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);
    private static readonly Regex _rgbRegex = new(@"^rgb\s*\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex _hslRegex = new(@"^hsl\s*\(\s*(\d{1,3})\s*,\s*(\d{1,3})%?\s*,\s*(\d{1,3})%?\s*\)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var text = subQuery.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return new SearchResult
            {
                Id         = "action:color:empty",
                Type       = ResultType.Action,
                Name       = "Color Converter",
                Subtitle   = "Type a Hex, RGB, or HSL color",
                IconGlyph  = IconGlyph,
                ActionId   = Id,
            };
            yield break;
        }

        var (success, hex, rgb, hsl) = ParseColor(text);
        if (!success)
        {
            yield return new SearchResult
            {
                Id         = "action:color:invalid",
                Type       = ResultType.Action,
                Name       = "Invalid color format",
                Subtitle   = "Try #ff0000, rgb(255,0,0), or hsl(0,100,50)",
                IconGlyph  = IconGlyph,
                ActionId   = Id,
            };
            yield break;
        }

        if (!text.StartsWith("#") && !text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
        {
            // If they typed just "ff0000", show hex first
            yield return MakeResult("hex", hex, "Hex");
            yield return MakeResult("rgb", rgb, "RGB");
            yield return MakeResult("hsl", hsl, "HSL");
        }
        else if (text.StartsWith("#"))
        {
            yield return MakeResult("rgb", rgb, "RGB");
            yield return MakeResult("hsl", hsl, "HSL");
        }
        else if (text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            yield return MakeResult("hex", hex, "Hex");
            yield return MakeResult("hsl", hsl, "HSL");
        }
        else
        {
            yield return MakeResult("hex", hex, "Hex");
            yield return MakeResult("rgb", rgb, "RGB");
        }
    }

    private SearchResult MakeResult(string type, string value, string label) => new()
    {
        Id         = $"action:color:{type}:{value}",
        Type       = ResultType.Action,
        Name       = value,
        Subtitle   = $"Copy {label} to clipboard",
        IconGlyph  = IconGlyph,
        ActionId   = Id,
    };

    public bool CanHandle(string query) => false;
    public SearchResult BuildResult(string query) => new();

    public Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var text = input.Trim();
        
        // When executing, the input might be the full query or the specific format they chose to copy.
        // If it starts with action:color, we need to extract the value.
        // MainViewModel usually passes the ActionInput which for color was just the query string, 
        // so we'd have to parse it. 
        // But since we want to return the result, let's just parse the input.
        
        var (success, hex, rgb, hsl) = ParseColor(text);
        if (!success)
            return Task.FromResult(new AddOnResult { Success = false });

        // Default copy hex
        var copyVal = hex;

        return Task.FromResult(new AddOnResult
        {
            Success = true,
            Title = Name,
            Detail = text,
            CopyText = copyVal,
            PanelId = Id,
            SubText = "Color copied"
        });
    }

    private static (bool success, string hex, string rgb, string hsl) ParseColor(string input)
    {
        var mHex = _hexRegex.Match(input);
        if (mHex.Success)
        {
            var val = mHex.Groups[1].Value;
            if (val.Length == 3) val = new string(new[] { val[0], val[0], val[1], val[1], val[2], val[2] });
            
            var r = int.Parse(val.Substring(0, 2), NumberStyles.HexNumber);
            var g = int.Parse(val.Substring(2, 2), NumberStyles.HexNumber);
            var b = int.Parse(val.Substring(4, 2), NumberStyles.HexNumber);
            
            return GenerateAll(r, g, b);
        }

        var mRgb = _rgbRegex.Match(input);
        if (mRgb.Success)
        {
            if (int.TryParse(mRgb.Groups[1].Value, out var r) && r <= 255 &&
                int.TryParse(mRgb.Groups[2].Value, out var g) && g <= 255 &&
                int.TryParse(mRgb.Groups[3].Value, out var b) && b <= 255)
            {
                return GenerateAll(r, g, b);
            }
        }

        var mHsl = _hslRegex.Match(input);
        if (mHsl.Success)
        {
            if (int.TryParse(mHsl.Groups[1].Value, out var h) && h <= 360 &&
                int.TryParse(mHsl.Groups[2].Value, out var s) && s <= 100 &&
                int.TryParse(mHsl.Groups[3].Value, out var l) && l <= 100)
            {
                var (r, g, b) = HslToRgb(h, s, l);
                return GenerateAll(r, g, b);
            }
        }

        return (false, "", "", "");
    }

    private static (bool, string, string, string) GenerateAll(int r, int g, int b)
    {
        var hex = $"#{r:X2}{g:X2}{b:X2}";
        var rgb = $"rgb({r}, {g}, {b})";
        
        double rf = r / 255.0;
        double gf = g / 255.0;
        double bf = b / 255.0;
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
        
        return (true, hex, rgb, hsl);
    }

    private static (int, int, int) HslToRgb(int h, int s, int l)
    {
        double hf = h / 360.0;
        double sf = s / 100.0;
        double lf = l / 100.0;
        
        double r, g, b;

        if (sf == 0)
        {
            r = g = b = lf;
        }
        else
        {
            double q = lf < 0.5 ? lf * (1 + sf) : lf + sf - lf * sf;
            double p = 2 * lf - q;
            r = HueToRgb(p, q, hf + 1.0 / 3.0);
            g = HueToRgb(p, q, hf);
            b = HueToRgb(p, q, hf - 1.0 / 3.0);
        }

        return ((int)Math.Round(r * 255), (int)Math.Round(g * 255), (int)Math.Round(b * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
        return p;
    }
}
