using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.Extras.Currency;

public sealed class CurrencyExtra : IExtra
{
    public string Id => "cur";
    public string Name => "Currency";
    public string Description => "Real-time currency conversion.";
    public string IconGlyph => "\ue12c";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "cur";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView()
    {
        return new CurrencySettingsView { DataContext = Settings };
    }

    private static readonly Regex _regex = new(
        @"^(?<amount>\d+(?:\.\d+)?)\s*(?<from>[a-z]{3})(?:\s+(?:to|in)\s+)?(?<to>[a-z]{3})?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
    private static readonly HttpClient _http = new();
    private static DateTime _lastFetch = DateTime.MinValue;
    private static Dictionary<string, double>? _rates;

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var text = subQuery.Trim();
        var settings = (Settings as CurrencySettings) ?? new CurrencySettings();
        
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return new SearchResult
            {
                Id         = "action:cur:empty",
                Type       = ResultType.Action,
                Name       = "Currency Converter",
                Subtitle   = $"e.g. 100 {settings.DefaultFrom} to {settings.DefaultTo}",
                IconGlyph  = IconGlyph,
                ActionId   = Id,
            };
            yield break;
        }

        var m = _regex.Match(text);
        if (m.Success)
        {
            var amount = double.Parse(m.Groups["amount"].Value, CultureInfo.InvariantCulture);
            var from = m.Groups["from"].Value.ToUpperInvariant();
            var to = m.Groups["to"].Success ? m.Groups["to"].Value.ToUpperInvariant() : settings.DefaultTo.ToUpperInvariant();

            yield return new SearchResult
            {
                Id         = $"action:cur:{amount}:{from}:{to}",
                Type       = ResultType.Action,
                Name       = $"Convert {amount} {from} to {to}",
                Subtitle   = "Press ↵ to calculate",
                IconGlyph  = IconGlyph,
                ActionId   = Id,
            };
        }
        else
        {
            yield return new SearchResult
            {
                Id         = "action:cur:invalid",
                Type       = ResultType.Action,
                Name       = "Invalid format",
                Subtitle   = $"e.g. 100 {settings.DefaultFrom} to {settings.DefaultTo}",
                IconGlyph  = IconGlyph,
                ActionId   = Id,
            };
        }
    }

    public bool CanHandle(string query) => false;
    public SearchResult BuildResult(string query) => new();

    public async Task<ExtraResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var settings = (Settings as CurrencySettings) ?? new CurrencySettings();
        var text = input.Trim();
        var m = _regex.Match(text);
        
        if (!m.Success)
            return new ExtraResult { Success = false, Title = Name, Detail = "Invalid format" };

        var amount = double.Parse(m.Groups["amount"].Value, CultureInfo.InvariantCulture);
        var from = m.Groups["from"].Value.ToUpperInvariant();
        var to = m.Groups["to"].Success ? m.Groups["to"].Value.ToUpperInvariant() : settings.DefaultTo.ToUpperInvariant();

        try
        {
            if (_rates == null || (DateTime.Now - _lastFetch).TotalHours > 24)
            {
                // Open.ER-API is a free, no-key public API for exchange rates
                var json = await _http.GetStringAsync("https://open.er-api.com/v6/latest/USD", ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.GetProperty("result").GetString() == "success")
                {
                    var ratesProp = root.GetProperty("rates");
                    _rates = new Dictionary<string, double>();
                    foreach (var prop in ratesProp.EnumerateObject())
                    {
                        _rates[prop.Name] = prop.Value.GetDouble();
                    }
                    _lastFetch = DateTime.Now;
                }
            }

            if (_rates != null && _rates.TryGetValue(from, out var rateFrom) && _rates.TryGetValue(to, out var rateTo))
            {
                var usdAmount = amount / rateFrom;
                var result = usdAmount * rateTo;
                
                var formatted = $"{result:N2} {to}";
                return new ExtraResult
                {
                    Success = true,
                    Title = Name,
                    Detail = formatted,
                    CopyText = formatted,
                    PanelId = Id,
                    SubText = "Copied to clipboard"
                };
            }
            
            return new ExtraResult { Success = false, Title = Name, Detail = "Currency not found" };
        }
        catch (Exception ex)
        {
            return new ExtraResult { Success = false, Title = Name, Detail = $"Error: {ex.Message}" };
        }
    }
}
