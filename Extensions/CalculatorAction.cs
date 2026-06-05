using System.Text.RegularExpressions;
using Spur.Models;

namespace Spur.Extensions;

public sealed partial class CalculatorAction : IAction
{
    public string Id => "calc";
    public string Name => "Calculator";
    public string IconGlyph => "\ue1d0";
    public bool IsGlobal => true;

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
                IconGlyph = "\ue1d0",
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
                IconGlyph = "\ue1d0",
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
        var dt = new System.Data.DataTable();
        return Convert.ToDouble(dt.Compute(expression, null));
    }
}
