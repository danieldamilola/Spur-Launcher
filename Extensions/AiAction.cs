namespace Arc.Extensions;

/// <summary>AI action. Triggered by "ai " followed by any non-empty question.</summary>
public sealed class AiAction : IAction
{
    public string Id => "ai";

    private static readonly Regex _trigger = new(
        @"^ai\s+.+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanHandle(string query) =>
        !string.IsNullOrWhiteSpace(query) && _trigger.IsMatch(query.Trim());

    public SearchResult BuildResult(string query)
    {
        var question = Regex.Replace(query.Trim(), @"^ai\s+", "", RegexOptions.IgnoreCase);
        var preview = question.Length > 50 ? question[..50] + "…" : question;

        return new SearchResult
        {
            Id       = "action:ai",
            Type     = ResultType.Action,
            Name     = "AI Assistant",
            Subtitle = $"{preview}  —  Press ↵ to ask",
            LucideIcon = "sparkles",
            ActionId = Id,
        };
    }

    /// <summary>Extracts the question portion of the query (strips "ai " prefix).</summary>
    public static string ExtractQuestion(string query) =>
        Regex.Replace(query.Trim(), @"^ai\s+", "", RegexOptions.IgnoreCase);
}

