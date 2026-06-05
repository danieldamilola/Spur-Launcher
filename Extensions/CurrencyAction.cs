using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Spur.Extensions;

/// <summary>
/// Currency conversion action. Triggered by "100 usd to eur" or "50 gbp to jpy".
/// Uses the free frankfurter.app API (no API key required).
/// </summary>
public sealed class CurrencyAction : IAction
{
    public string Id => "currency";
    public string Name => "Currency";
    public string IconGlyph => "\ue825";
    public bool IsGlobal => false;

    private static readonly Regex _trigger = new(
        @"^([\d,.]+)\s*([a-zA-Z]{3})\s+(?:to|in)\s+([a-zA-Z]{3})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://api.frankfurter.app/"),
        Timeout = TimeSpan.FromSeconds(5),
    };

    // ── Keyword-scoped ────────────────────────────────────────────
    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var text = subQuery.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            yield return new SearchResult
            {
                Id         = "action:currency",
                Type       = ResultType.Action,
                Name       = "Currency Converter",
                Subtitle   = "Type an amount like 100 usd to eur",
                IconGlyph  = "\ue825",
                ActionId   = Id,
            };
            yield break;
        }

        var (amount, from, to) = Parse(text);
        if (amount is not null)
        {
            yield return new SearchResult
            {
                Id         = $"action:currency:{from}-{to}",
                Type       = ResultType.Action,
                Name       = $"Convert {amount:N2} {from.ToUpperInvariant()} → {to.ToUpperInvariant()}",
                Subtitle   = "Press ↵ to fetch rate and copy",
                IconGlyph  = "\ue825",
                ActionId   = Id,
            };
        }
        else
        {
            yield return new SearchResult
            {
                Id         = "action:currency",
                Type       = ResultType.Action,
                Name       = "Currency Converter",
                Subtitle   = "Format: 100 usd to eur",
                IconGlyph  = "\ue825",
                ActionId   = Id,
            };
        }
    }

    // ── Legacy (global) ───────────────────────────────────────────
    public bool CanHandle(string query)
        => !string.IsNullOrWhiteSpace(query) && _trigger.IsMatch(query.Trim());

    public SearchResult BuildResult(string query)
    {
        var (amount, fromCurr, toCurr) = Parse(query);
        if (amount is null) return new() { Id = "action:currency", Type = ResultType.Action, Name = "Currency", ActionId = Id };

        return new SearchResult
        {
            Id         = $"action:currency:{fromCurr}-{toCurr}",
            Type       = ResultType.Action,
            Name       = $"Convert {amount:N2} {fromCurr.ToUpperInvariant()} → {toCurr.ToUpperInvariant()}",
            Subtitle   = "Press ↵ to fetch rate and copy",
            IconGlyph = "\ue825",
            ActionId   = Id,
        };
    }

    private static (decimal? Amount, string From, string To) Parse(string query)
    {
        var m = _trigger.Match(query.Trim());
        if (!m.Success) return (null, "", "");

        var amountStr = m.Groups[1].Value.Replace(",", "");
        if (!decimal.TryParse(amountStr, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var amount))
            return (null, "", "");

        return (amount, m.Groups[2].Value, m.Groups[3].Value);
    }

    public static async Task<string?> ConvertAsync(string query, CancellationToken ct = default)
    {
        var (amount, from, to) = Parse(query);
        if (amount is null) return null;

        try
        {
            var url = $"latest?amount={amount}&from={from.ToUpperInvariant()}&to={to.ToUpperInvariant()}";
            var response = await _http.GetFromJsonAsync<FrankfurterResponse>(url, ct);

            if (response?.Rates?.TryGetValue(to.ToUpperInvariant(), out var result) == true)
                return $"{amount:N2} {from.ToUpperInvariant()} = {result:N2} {to.ToUpperInvariant()}";

            return $"Could not convert {from.ToUpperInvariant()} → {to.ToUpperInvariant()}";
        }
        catch (Exception ex)
        {
            return $"Currency API error: {ex.Message}";
        }
    }

    private sealed class FrankfurterResponse
    {
        [JsonPropertyName("rates")]
        public Dictionary<string, decimal>? Rates { get; set; }
    }
}

