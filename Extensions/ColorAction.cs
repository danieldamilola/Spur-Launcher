using System.Text.RegularExpressions;
using Spur.Models;

namespace Spur.Extensions;

public sealed partial class ColorAction : IAction
{
    public string Id => "color";
    public string Name => "Color";
    public string IconGlyph => "\ue790";
    public bool IsGlobal => false;

    [GeneratedRegex(@"^#([0-9a-fA-F]{6}|[0-9a-fA-F]{3})$")]
    private static partial Regex HexPattern();

    public bool CanHandle(string query) =>
        !string.IsNullOrWhiteSpace(query) && HexPattern().IsMatch(query.Trim());

    public SearchResult BuildResult(string query)
    {
        var hex = query.Trim();
        return new SearchResult
        {
            Id = $"color:{hex}",
            Type = ResultType.Action,
            Name = hex.ToUpper(),
            Subtitle = "Color",
            IconGlyph = "\ue790",
            ActionId = Id,
            Score = 800,
        };
    }

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        if (string.IsNullOrWhiteSpace(subQuery))
        {
            yield return new SearchResult
            {
                Id = "action:color",
                Type = ResultType.Action,
                Name = "Color Picker",
                Subtitle = "Type a hex code like #FF5733",
                IconGlyph = "\ue790",
                ActionId = Id,
            };
            yield break;
        }
        if (CanHandle(subQuery.Trim()))
            yield return BuildResult(subQuery.Trim());
    }
}
