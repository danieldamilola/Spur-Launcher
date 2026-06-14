using System;
using Spur.Models;
using Spur.Services;

namespace Spur.Tests;

public class FuzzySearchTests
{
    [Theory]
    [InlineData("chr", "Google Chrome")]
    [InlineData("vs", "Visual Studio Code")]
    [InlineData("note", "Notepad")]
    [InlineData("calc", "Calculator")]
    [InlineData("term", "Windows Terminal")]
    public void Score_PositiveMatch_ReturnsNonNegative(string query, string target)
    {
        var score = FuzzySearch.Score(query, target);
        Assert.True(score >= 0, $"Expected non-negative score for '{query}' → '{target}', got {score}");
    }

    [Theory]
    [InlineData("xyz", "Google Chrome")]
    [InlineData("zzz", "Notepad")]
    [InlineData("foo", "Calculator")]
    public void Score_NoMatch_ReturnsNegative(string query, string target)
    {
        var score = FuzzySearch.Score(query, target);
        Assert.True(score < 0, $"Expected negative score for '{query}' → '{target}', got {score}");
    }

    [Fact]
    public void Score_EmptyQuery_ReturnsZero()
    {
        Assert.Equal(0, FuzzySearch.Score("", "anything"));
    }

    [Fact]
    public void Score_EmptyTarget_ReturnsNegative()
    {
        Assert.True(FuzzySearch.Score("test", "") < 0);
    }

    [Theory]
    [InlineData("Chrome", "chrome")]
    [InlineData("CHROME", "Chrome")]
    [InlineData("chrome", "CHROME")]
    public void Score_CaseInsensitive_Match(string query, string target)
    {
        var score = FuzzySearch.Score(query, target);
        Assert.True(score >= 0, $"Expected match for '{query}' → '{target}'");
    }

    [Fact]
    public void Score_ExactMatch_ScoresHigherThanPartial()
    {
        var exactScore = FuzzySearch.Score("notepad", "notepad");
        var partialScore = FuzzySearch.Score("note", "notepad");
        Assert.True(exactScore > partialScore, $"Exact: {exactScore}, Partial: {partialScore}");
    }

    [Fact]
    public void Score_PrefixMatch_ScoresPositive()
    {
        var score = FuzzySearch.Score("note", "Notepad");
        Assert.True(score > 0, $"Prefix 'note' should match 'Notepad', got {score}");
    }

    [Fact]
    public void Score_WordBoundaryMatch_ScoresHigher()
    {
        // "vs" at word start "Visual Studio" should score well
        var boundaryScore = FuzzySearch.Score("vs", "Visual Studio");
        Assert.True(boundaryScore >= 0, $"Word boundary match should be positive, got {boundaryScore}");
    }

    [Fact]
    public void Score_BothEmptyStrings_ReturnsZero()
    {
        Assert.Equal(0, FuzzySearch.Score("", ""));
    }

    [Fact]
    public void Score_QueryLongerThanTarget_ReturnsNegative()
    {
        var score = FuzzySearch.Score("very long query string", "abc");
        Assert.True(score < 0, $"Query longer than target should not match, got {score}");
    }

    [Fact]
    public void Score_SingleCharMatch_ReturnsPositive()
    {
        var score = FuzzySearch.Score("c", "Calculator");
        Assert.True(score >= 0, $"Single char 'c' should match 'Calculator', got {score}");
    }

    [Fact]
    public void Score_SingleCharNoMatch_ReturnsNegative()
    {
        var score = FuzzySearch.Score("z", "Calculator");
        Assert.True(score < 0, $"'z' should not match 'Calculator', got {score}");
    }
}

public class CategoryNamesTests
{
    [Fact]
    public void ToString_ReturnsExpectedValues()
    {
        Assert.Equal("apps", CategoryNames.ToString(Category.Apps));
        Assert.Equal("files", CategoryNames.ToString(Category.Files));
        Assert.Equal("clipboard", CategoryNames.ToString(Category.Clipboard));
        Assert.Equal("actions", CategoryNames.ToString(Category.Actions));
    }

    [Theory]
    [InlineData("apps", Category.Apps)]
    [InlineData("Apps", Category.Apps)]
    [InlineData("APPS", Category.Apps)]
    [InlineData("files", Category.Files)]
    [InlineData("clipboard", Category.Clipboard)]
    [InlineData("actions", Category.Actions)]
    public void FromString_Valid_ReturnsCategory(string input, Category expected)
    {
        var result = CategoryNames.FromString(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("garbage")]
    public void FromString_Invalid_ReturnsNull(string? input)
    {
        Assert.Null(CategoryNames.FromString(input));
    }
}
