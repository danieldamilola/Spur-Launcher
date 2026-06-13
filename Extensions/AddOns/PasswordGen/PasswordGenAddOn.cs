using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Spur.Models;

namespace Spur.Extensions.AddOns.PasswordGen;

public sealed class PasswordGenAddOn : IAddOn
{
    public string Id => "pw";
    public string Name => "Password";
    public string Description => "Generate strong random passwords.";
    public string IconGlyph => "\ue8d7";
    public string? IconPath => "/Assets/Icons/lock.png";
    public string Author => "Built-in";
    public string Version => "2.0.0";
    public bool IsBuiltIn => true;

    public bool IsEnabled { get; set; } = true;
    public string Keyword { get; set; } = "pw";
    public bool IsGlobal => false;

    public object? Settings { get; set; }
    public FrameworkElement? CreateSettingsView()
    {
        return new PasswordGenSettingsView { DataContext = Settings };
    }

    private const int MaxLength = 128;
    private const string LowerChars = "abcdefghijklmnopqrstuvwxyz";
    private const string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string NumberChars = "0123456789";
    private const string SymbolChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

    private static readonly Regex _trigger = new(
        @"^pw(?:\s+(\d{1,3}))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IEnumerable<SearchResult> GetResults(string subQuery)
    {
        var text = subQuery.Trim();
        var settings = (Settings as PasswordGenSettings) ?? new PasswordGenSettings();
        var defaultLength = settings.DefaultLength;

        if (string.IsNullOrWhiteSpace(text))
        {
            yield return new SearchResult
            {
                Id         = $"action:pw:{defaultLength}",
                Type       = ResultType.Action,
                Name       = $"Generate Password · {defaultLength} chars",
                Subtitle   = "Press ↵ to generate and copy",
                IconGlyph  = IconGlyph,
                IconPath   = IconPath,
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
                IconGlyph  = IconGlyph,
                IconPath   = IconPath,
                ActionId   = Id,
            };
        }
        else
        {
            yield return new SearchResult
            {
                Id         = $"action:pw:{defaultLength}",
                Type       = ResultType.Action,
                Name       = $"Generate Password · {defaultLength} chars",
                Subtitle   = "Invalid length, press ↵ for default",
                IconGlyph  = IconGlyph,
                IconPath   = IconPath,
                ActionId   = Id,
            };
        }
    }

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
            IconGlyph  = IconGlyph,
            IconPath   = IconPath,
            ActionId   = Id,
        };
    }

    private int ParseLength(string query)
    {
        var settings = (Settings as PasswordGenSettings) ?? new PasswordGenSettings();
        var trimmed = query.Trim();
        if (int.TryParse(trimmed, out var rawLen) && rawLen > 0)
            return Math.Min(rawLen, MaxLength);

        var m = _trigger.Match(trimmed);
        return m.Success && int.TryParse(m.Groups[1].Value, out var len) && len > 0
            ? Math.Min(len, MaxLength) : settings.DefaultLength;
    }

    private static string Generate(int length, bool includeSymbols, bool includeNumbers, bool includeUppercase)
    {
        length = Math.Clamp(length, 1, MaxLength);
        var chars = LowerChars
            + (includeUppercase ? UpperChars : "")
            + (includeNumbers ? NumberChars : "")
            + (includeSymbols ? SymbolChars : "");

        if (string.IsNullOrEmpty(chars))
            chars = LowerChars;

        var bytes = RandomNumberGenerator.GetBytes(length * 4);
        var password = new char[length];
        for (int i = 0; i < length; i++)
        {
            var idx = BitConverter.ToUInt32(bytes, i * 4) % (uint)chars.Length;
            password[i] = chars[(int)idx];
        }
        return new string(password);
    }

    public Task<AddOnResult> ExecuteAsync(string input, CancellationToken ct = default)
    {
        var settings = (Settings as PasswordGenSettings) ?? new PasswordGenSettings();
        var length = ParseLength(input);
        
        var pw = Generate(length, settings.IncludeSymbols, settings.IncludeNumbers, settings.IncludeUppercase);

        return Task.FromResult(new AddOnResult
        {
            Success = true,
            Title = Name,
            Detail = "Generated and copied",
            CopyText = pw,
            PanelId = Id,
            SubText = "Password copied to clipboard"
        });
    }
}
