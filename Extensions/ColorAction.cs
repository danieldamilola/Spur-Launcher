namespace Volt.Extensions;

/// <summary>
/// Color action. Triggered when the query is a hex color like #FF5733 or #abc.
/// Computes and exposes HEX, RGB, and HSL values.
/// </summary>
public sealed class ColorAction : IAction
{
    public string Id => "color";

    private static readonly Regex _trigger = new(
        @"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$", RegexOptions.Compiled);

    public bool CanHandle(string query) =>
        !string.IsNullOrWhiteSpace(query) && _trigger.IsMatch(query.Trim());

    public SearchResult BuildResult(string query)
    {
        var hex = Normalize(query.Trim());
        ParseHex(hex, out var r, out var g, out var b);
        RgbToHsl(r, g, b, out var h, out var s, out var l);

        return new SearchResult
        {
            Id       = "action:color",
            Type     = ResultType.Action,
            Name     = hex.ToUpperInvariant(),
            Subtitle = $"RGB({r},{g},{b})  HSL({h:F0}°,{s:F0}%,{l:F0}%)",
            LucideIcon = "palette",
            ActionId = Id,
        };
    }

    /// <summary>Expands shorthand (#abc → #aabbcc) and returns lowercase full hex.</summary>
    public static string Normalize(string raw)
    {
        var s = raw.TrimStart('#');
        if (s.Length == 3)
            s = string.Concat(s.Select(c => $"{c}{c}"));
        return $"#{s.ToLowerInvariant()}";
    }

    public static void ParseHex(string hex, out byte r, out byte g, out byte b)
    {
        var s = hex.TrimStart('#');
        r = Convert.ToByte(s[..2], 16);
        g = Convert.ToByte(s[2..4], 16);
        b = Convert.ToByte(s[4..6], 16);
    }

    public static void RgbToHsl(byte r, byte g, byte b,
        out double h, out double s, out double l)
    {
        double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
        double max = Math.Max(rf, Math.Max(gf, bf));
        double min = Math.Min(rf, Math.Min(gf, bf));
        double delta = max - min;

        l = (max + min) / 2.0 * 100.0;

        if (delta == 0) { h = s = 0; return; }

        s = (l < 50 ? delta / (max + min) : delta / (2 - max - min)) * 100.0;

        if (max == rf)      h = ((gf - bf) / delta % 6) * 60;
        else if (max == gf) h = ((bf - rf) / delta + 2) * 60;
        else                h = ((rf - gf) / delta + 4) * 60;

        if (h < 0) h += 360;
    }
}
