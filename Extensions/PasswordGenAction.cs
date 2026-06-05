using System.Security.Cryptography;

namespace Spur.Extensions;

/// <summary>
/// Password Generator action. Triggered by "pw [length]", e.g. "pw 16" or "pw".
/// Generates a cryptographically random password and copies to clipboard on Enter.
/// </summary>
public sealed class PasswordGenAction : IAction
{
    public string Id => "pw";
    public string Name => "Password";
    public string IconGlyph => "\ue8d7";
    public bool IsGlobal => false;

    private const int DefaultLength = 16;
    private const int MaxLength = 128;
    private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";

    private static readonly Regex _trigger = new(
        @"^pw(?:\s+(\d{1,3}))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ── Keyword-scoped ────────────────────────────────────────────
    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var text = subQuery.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            yield return new SearchResult
            {
                Id         = $"action:pw:{DefaultLength}",
                Type       = ResultType.Action,
                Name       = $"Generate Password · {DefaultLength} chars",
                Subtitle   = "Press ↵ to generate and copy",
                IconGlyph  = "\ue8d7",
                ActionId   = Id,
            };
            yield break;
        }

        if (int.TryParse(text, out var length) && length > 0)
        {
            length = Math.Min(length, MaxLength);
            yield return new SearchResult
            {
                Id         = $"action:pw:{length}",
                Type       = ResultType.Action,
                Name       = $"Generate Password · {length} chars",
                Subtitle   = "Press ↵ to generate and copy",
                IconGlyph  = "\ue8d7",
                ActionId   = Id,
            };
        }
        else
        {
            yield return new SearchResult
            {
                Id         = $"action:pw:{DefaultLength}",
                Type       = ResultType.Action,
                Name       = $"Generate Password · {DefaultLength} chars",
                Subtitle   = "Invalid length, press ↵ for default",
                IconGlyph  = "\ue8d7",
                ActionId   = Id,
            };
        }
    }

    // ── Legacy (global) ───────────────────────────────────────────
    public bool CanHandle(string query)
        => !string.IsNullOrWhiteSpace(query) && _trigger.IsMatch(query.Trim());

    public SearchResult BuildResult(string query)
    {
        var length = ParseLength(query);
        return new SearchResult
        {
            Id         = $"action:pw:{length}",
            Type       = ResultType.Action,
            Name       = $"Generate Password · {length} chars",
            Subtitle   = "Press ↵ to generate and copy",
            IconGlyph = "\ue8d7",
            ActionId   = Id,
        };
    }

    private static int ParseLength(string query)
    {
        var m = _trigger.Match(query.Trim());
        return m.Success && int.TryParse(m.Groups[1].Value, out var len) && len > 0
            ? Math.Min(len, MaxLength) : DefaultLength;
    }

    public static string Generate(string query)
    {
        var length = ParseLength(query);
        return Generate(length);
    }

    public static string Generate(int length = DefaultLength)
    {
        length = Math.Clamp(length, 1, MaxLength);
        var bytes = RandomNumberGenerator.GetBytes(length * 4);
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            var idx = BitConverter.ToUInt32(bytes, i * 4) % (uint)Chars.Length;
            chars[i] = Chars[(int)idx];
        }
        return new string(chars);
    }
}

