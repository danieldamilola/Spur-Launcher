using System.Data;
using Volt.Extensions;

namespace Volt.Extensions;

/// <summary>
/// Evaluates math expressions. Triggered when the entire query looks like a math expression.
/// Uses DataTable.Compute for safe, sandboxed evaluation.
/// </summary>
public sealed class CalculatorAction : IAction
{
    public string Id => "calc";

    // Must contain at least one digit and one operator, no letters
    private static readonly Regex _trigger = new(
        @"^[\d\s\+\-\*\/\%\(\)\.\,\^]+$", RegexOptions.Compiled);

    // Must have at least one operator to distinguish from plain numbers
    private static readonly Regex _hasOp = new(
        @"[\+\-\*\/\%\^]", RegexOptions.Compiled);

    public bool CanHandle(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;
        var q = query.Trim();
        return q.Any(char.IsDigit) && _trigger.IsMatch(q) && _hasOp.IsMatch(q);
    }

    public SearchResult BuildResult(string query)
    {
        var expression = query.Trim();
        var resultText = Evaluate(expression);

        return new SearchResult
        {
            Id       = "action:calc",
            Type     = ResultType.Action,
            Name     = resultText,
            Subtitle = expression,
            LucideIcon = "calculator",
            ActionId = Id,
        };
    }

    public static string Evaluate(string expression)
    {
        try
        {
            // DataTable.Compute doesn't support ^ — strip it
            var safe = Regex.Replace(expression, @"[^\d\s\+\-\*\/\%\(\)\.]", "").Trim();
            if (string.IsNullOrEmpty(safe)) return "Invalid expression";

            var result = new DataTable().Compute(safe, null);

            if (result is double d)
            {
                return d == Math.Floor(d) && !double.IsInfinity(d) && !double.IsNaN(d)
                    ? $"= {d:N0}"
                    : $"= {d:G10}";
            }
            return $"= {result}";
        }
        catch { return "Invalid expression"; }
    }
}
