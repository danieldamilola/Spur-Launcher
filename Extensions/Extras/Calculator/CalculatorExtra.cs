using System.Data;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.Extras.Calculator;

public sealed partial class CalculatorExtra : IExtra
{
    public string Id => "calc";
    public string Name => "Calculator";
    public string Description => "Evaluate math expressions inline.";
    public string IconGlyph => "\ue1d0";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "";
    public bool IsGlobal => true;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView() => null;

    [GeneratedRegex(@"^[\d+\-*/\s().^%e]+$")]
    private static partial Regex ExpressionPattern();

    public bool CanHandle(string query) =>
        !string.IsNullOrWhiteSpace(query) && ExpressionPattern().IsMatch(query.Trim());

    public SearchResult BuildResult(string query)
    {
        try
        {
            var expr = query.Trim();
            var result = Evaluate(expr);
            return new SearchResult
            {
                Id = $"calc:{expr}",
                Type = ResultType.Action,
                Name = $"= {result}",
                Subtitle = expr,
                IconGlyph = IconGlyph,
                ActionId = Id,
                Score = 1000,
            };
        }
        catch
        {
            return new SearchResult
            {
                Id = "calc:error",
                Type = ResultType.Action,
                Name = "Invalid expression",
                Subtitle = query,
                IconGlyph = IconGlyph,
                ActionId = Id,
            };
        }
    }

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        if (CanHandle(subQuery))
            yield return BuildResult(subQuery);
    }

    private static double Evaluate(string expression)
    {
        using var dt = new DataTable();
        return Convert.ToDouble(dt.Compute(expression, null));
    }

    public Task<ExtraResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        try
        {
            var expr = input.Trim();
            var result = Evaluate(expr).ToString();
            return Task.FromResult(new ExtraResult
            {
                Success = true,
                Title = Name,
                Detail = result,
                CopyText = result,
                PanelId = Id,
                SubText = "Copied to clipboard"
            });
        }
        catch
        {
            return Task.FromResult(new ExtraResult
            {
                Success = false,
                Title = Name,
                Detail = "Invalid expression"
            });
        }
    }
}
