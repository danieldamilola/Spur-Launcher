using Spur.Models;

namespace Spur.Extensions;

public sealed class AiAction : IAction
{
    public string Id => "ai";
    public string Name => "AI";
    public string IconGlyph => "AI";
    public bool IsGlobal => false;

    public bool CanHandle(string query) =>
        !string.IsNullOrWhiteSpace(query) && query.Trim().StartsWith("ai ", StringComparison.OrdinalIgnoreCase);

    public SearchResult BuildResult(string query)
    {
        var prompt = query.Trim();
        if (prompt.StartsWith("ai ", StringComparison.OrdinalIgnoreCase))
            prompt = prompt[3..].Trim();
        return new SearchResult
        {
            Id = $"ai:{prompt}",
            Type = ResultType.Action,
            Name = $"Ask: {prompt}",
            Subtitle = "AI",
            IconGlyph = "AI",
            ActionId = Id,
            Score = 600,
        };
    }

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        if (string.IsNullOrWhiteSpace(subQuery))
        {
            yield return new SearchResult
            {
                Id = "action:ai",
                Type = ResultType.Action,
                Name = "Ask AI",
                Subtitle = "Type your question…",
                IconGlyph = "AI",
                ActionId = Id,
            };
            yield break;
        }
        yield return BuildResult($"ai {subQuery}");
    }

    public static string ExtractQuestion(string query)
    {
        var trimmed = query.Trim();
        if (trimmed.StartsWith("ai ", StringComparison.OrdinalIgnoreCase))
            return trimmed[3..].Trim();
        return trimmed;
    }
}
