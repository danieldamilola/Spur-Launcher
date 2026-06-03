using Spur.Models;
using Xunit;

namespace Spur.Tests;

public class SpurConfigValidationTests
{
    [Fact]
    public void Validate_ClampsWindowOpacityOutOfRange()
    {
        var cfg = new SpurConfig { WindowOpacity = 2.0 };
        cfg.Validate();
        Assert.Equal(1.0, cfg.WindowOpacity);
    }

    [Fact]
    public void Validate_ClampsWindowOpacityBelowMin()
    {
        var cfg = new SpurConfig { WindowOpacity = 0.1 };
        cfg.Validate();
        Assert.Equal(0.3, cfg.WindowOpacity);
    }

    [Fact]
    public void Validate_LeavesWindowOpacityInRange()
    {
        var cfg = new SpurConfig { WindowOpacity = 0.85 };
        cfg.Validate();
        Assert.Equal(0.85, cfg.WindowOpacity);
    }

    [Fact]
    public void Validate_ClampsResultsCountOutOfRange()
    {
        var cfg = new SpurConfig { ResultsCount = 100 };
        cfg.Validate();
        Assert.Equal(20, cfg.ResultsCount);
    }

    [Fact]
    public void Validate_ClampsResultsCountBelowMin()
    {
        var cfg = new SpurConfig { ResultsCount = 0 };
        cfg.Validate();
        Assert.Equal(3, cfg.ResultsCount);
    }

    [Fact]
    public void Validate_ClampsMaxFileDepth()
    {
        var cfg = new SpurConfig { MaxFileDepth = 10 };
        cfg.Validate();
        Assert.Equal(5, cfg.MaxFileDepth);
    }

    [Fact]
    public void Validate_ClampsMaxFileDepthBelowMin()
    {
        var cfg = new SpurConfig { MaxFileDepth = 0 };
        cfg.Validate();
        Assert.Equal(1, cfg.MaxFileDepth);
    }

    [Fact]
    public void Validate_ClampsClipboardHistorySize()
    {
        var cfg = new SpurConfig { ClipboardHistorySize = 999 };
        cfg.Validate();
        Assert.Equal(200, cfg.ClipboardHistorySize);
    }

    [Fact]
    public void Validate_ClampsClipboardHistorySizeBelowMin()
    {
        var cfg = new SpurConfig { ClipboardHistorySize = 1 };
        cfg.Validate();
        Assert.Equal(5, cfg.ClipboardHistorySize);
    }

    [Fact]
    public void Validate_NullPinnedClipboard_BecomesEmpty()
    {
        var cfg = new SpurConfig { PinnedClipboard = null! };
        cfg.Validate();
        Assert.NotNull(cfg.PinnedClipboard);
        Assert.Empty(cfg.PinnedClipboard);
    }

    [Fact]
    public void Clone_ProducesEqualButNotSameObject()
    {
        var original = new SpurConfig
        {
            Theme = "light",
            ResultsCount = 8,
            IndexedFolders = new() { @"C:\Projects", @"D:\Docs" },
            PinnedItems = new(StringComparer.OrdinalIgnoreCase) { "app:notepad" },
        };
        original.Validate();

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        Assert.Equal(original.Theme, clone.Theme);
        Assert.Equal(original.ResultsCount, clone.ResultsCount);
        Assert.Equal(original.IndexedFolders, clone.IndexedFolders);
        Assert.NotSame(original.IndexedFolders, clone.IndexedFolders);
    }

    [Fact]
    public void Clone_ModifyingCloneDoesNotAffectOriginal()
    {
        var original = new SpurConfig { Theme = "dark" };
        var clone = original.Clone();

        clone.Theme = "light";

        Assert.Equal("dark", original.Theme);
        Assert.Equal("light", clone.Theme);
    }

    [Fact]
    public void ActiveApiKey_RoutesByProvider()
    {
        var cfg = new SpurConfig
        {
            AiProvider = "groq",
            EncryptedGroqApiKey = "UExBSU46dGVzdF9ncm9xX2tleQ==",   // PLAIN:test_groq_key
            EncryptedGeminiApiKey = "UExBSU46dGVzdF9nZW1pbmlfa2V5",   // PLAIN:test_gemini_key
        };

        Assert.Equal("test_groq_key", cfg.ActiveApiKey);

        cfg.AiProvider = "gemini";
        Assert.Equal("test_gemini_key", cfg.ActiveApiKey);
    }

    [Fact]
    public void ActiveApiKey_UnknownProvider_ReturnsEmpty()
    {
        var cfg = new SpurConfig { AiProvider = "nonexistent" };
        Assert.Equal(string.Empty, cfg.ActiveApiKey);
    }

    [Fact]
    public void ActiveModel_RoutesByProvider()
    {
        var cfg = new SpurConfig
        {
            AiProvider = "groq",
            GroqModel = "llama-3.2-70b",
            OpenRouterModel = "openai/gpt-4o",
        };

        Assert.Equal("llama-3.2-70b", cfg.ActiveModel);

        cfg.AiProvider = "openrouter";
        Assert.Equal("openai/gpt-4o", cfg.ActiveModel);
    }

    [Fact]
    public void FileSearchEnabled_AliasMirrorsIndexFiles()
    {
        var cfg = new SpurConfig();

        cfg.FileSearchEnabled = false;
        Assert.False(cfg.IndexFiles);

        cfg.IndexFiles = true;
        Assert.True(cfg.FileSearchEnabled);
    }

    [Fact]
    public void Defaults_AreSensible()
    {
        var cfg = new SpurConfig();

        Assert.Equal("dark", cfg.Theme);
        Assert.Equal(0.95, cfg.WindowOpacity, 3);
        Assert.Equal(5, cfg.ResultsCount);
        Assert.Equal("Alt+Space", cfg.Shortcut);
        Assert.True(cfg.FuzzySearch);
        Assert.True(cfg.LaunchOnStartup);
        Assert.True(cfg.IndexApps);
        Assert.Equal(3, cfg.MaxFileDepth);
        Assert.Equal(50, cfg.ClipboardHistorySize);
        Assert.NotNull(cfg.PinnedItems);
        Assert.NotNull(cfg.FileExtensions);
        Assert.NotEmpty(cfg.FileExtensions);
        Assert.NotNull(cfg.IndexedFolders);
        Assert.NotNull(cfg.ExcludedFolders);
    }
}

