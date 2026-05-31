using System.Security.Cryptography;

namespace Arc.Extensions;

/// <summary>
/// Password Generator action. Triggered by "pw [length]", e.g. "pw 16" or "pw".
/// Generates a cryptographically random password and copies to clipboard on Enter.
/// </summary>
public sealed class PasswordGenAction : IAction
{
    public string Id => "pw";

    private const int DefaultLength = 16;
    private const int MaxLength = 128;
    private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+-=[]{}|;:,.<>?";

    private static readonly Regex _trigger = new(
        @"^pw(?:\s+(\d{1,3}))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            LucideIcon = "key",
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
