using System.Collections.Generic;
using System;

namespace Spur.Services;

public readonly struct MatchResult(double score, HashSet<int> matchedIndices)
{
    public double Score { get; } = score;
    public IReadOnlySet<int> MatchedIndices { get; } = matchedIndices;
    public bool Success => Score >= 0;
}

public static class FuzzySearch
{
    public static double Score(string query, string target)
    {
        if (string.IsNullOrEmpty(query)) return 0;
        if (string.IsNullOrEmpty(target)) return -1;
        return ScoreCore(query.AsSpan(), target.AsSpan(), null, false);
    }

    public static double Score(ReadOnlySpan<char> query, ReadOnlySpan<char> target)
    {
        if (query.IsEmpty) return 0;
        if (target.IsEmpty) return -1;
        return ScoreCore(query, target, null, false);
    }

    public static MatchResult Match(string query, string target)
    {
        if (string.IsNullOrEmpty(query)) return new MatchResult(0, []);
        if (string.IsNullOrEmpty(target)) return new MatchResult(-1, []);
        var indices = new HashSet<int>();
        var score = ScoreCore(query.AsSpan(), target.AsSpan(), indices, true);
        return new MatchResult(score, indices);
    }

    public static MatchResult Match(ReadOnlySpan<char> query, ReadOnlySpan<char> target)
    {
        if (query.IsEmpty) return new MatchResult(0, []);
        if (target.IsEmpty) return new MatchResult(-1, []);
        var indices = new HashSet<int>();
        var score = ScoreCore(query, target, indices, true);
        return new MatchResult(score, indices);
    }

    private static double ScoreCore(
        ReadOnlySpan<char> query, ReadOnlySpan<char> target,
        HashSet<int>? matchedIndices, bool useTypoFallback)
    {
        // Normal fuzzy match — multi-word by splitting on spaces
        double totalScore = 0;
        bool anyTermMatched = false;
        int searchStart = 0;
        int termStart = 0;

        for (int i = 0; i <= query.Length; i++)
        {
            if (i == query.Length || query[i] == ' ')
            {
                if (i > termStart)
                {
                    var term = query[termStart..i];
                    if (term.Length > 0)
                    {
                        var (score, consumed) = MatchSingleTerm(term, target, searchStart, matchedIndices);
                        if (consumed > searchStart)
                        {
                            totalScore += score;
                            anyTermMatched = true;
                            searchStart = consumed;
                        }
                        else if (useTypoFallback && term.Length >= 3 && term.Length <= 10)
                        {
                            var typoScore = ScoreTypoFallback(term, target);
                            if (typoScore > 0)
                                totalScore += typoScore;
                            else
                                return -1;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }
                termStart = i + 1;
            }
        }

        if (!anyTermMatched) return -1;

        double acronymBonus = 0;
        var initials = ExtractInitials(target);
        if (initials.Length > 0 && query.Length <= initials.Length)
        {
            bool prefixMatch = true;
            for (int i = 0; i < query.Length; i++)
            {
                if (char.ToLowerInvariant(query[i]) != char.ToLowerInvariant(initials[i]))
                {
                    prefixMatch = false;
                    break;
                }
            }
            if (prefixMatch) acronymBonus = 50;
        }

        double coverageBonus = (double)query.Length / target.Length;
        return totalScore + coverageBonus + acronymBonus;
    }

    private static (double score, int consumedTo) MatchSingleTerm(
        ReadOnlySpan<char> term, ReadOnlySpan<char> target, int startAt, HashSet<int>? matchedIndices)
    {
        int qi = 0;
        int ti = startAt;
        int consecutive = 0;
        double score = 0;
        bool isFirstMatch = true;

        while (qi < term.Length && ti < target.Length)
        {
            if (char.ToLowerInvariant(term[qi]) == char.ToLowerInvariant(target[ti]))
            {
                consecutive++;
                matchedIndices?.Add(ti);

                if (isFirstMatch && IsWordBoundary(ti, target))
                {
                    score += 1.0;
                    isFirstMatch = false;
                }

                score += 0.1 * consecutive;
                qi++;
            }
            else
            {
                consecutive = 0;
            }

            ti++;
        }

        if (qi < term.Length)
            return (-1, startAt);

        return (score, ti);
    }

    private static double ScoreTypoFallback(ReadOnlySpan<char> query, ReadOnlySpan<char> target)
    {
        var queryStr = query.ToString();
        var targetStr = target.ToString();

        var separators = new[] { ' ', '-', '_', '.', '/', '\\' };
        var words = targetStr.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        int bestDistance = int.MaxValue;
        foreach (var word in words)
        {
            if (word.Length < query.Length - 2 || word.Length > query.Length + 2)
                continue;

            int distance = DamerauLevenshteinDistance(queryStr, word);
            if (distance < bestDistance)
                bestDistance = distance;
        }

        if (bestDistance <= 2)
            return 10.0 - bestDistance * 3.0;

        return -1;
    }

    public static int DamerauLevenshteinDistance(string source, string target)
    {
        int sLen = source.Length;
        int tLen = target.Length;

        if (sLen == 0) return tLen;
        if (tLen == 0) return sLen;

        var d = new int[(sLen + 1) * (tLen + 1)];
        int stride = tLen + 1;

        for (int i = 0; i <= sLen; i++) d[i * stride] = i;
        for (int j = 0; j <= tLen; j++) d[j] = j;

        for (int i = 1; i <= sLen; i++)
        {
            for (int j = 1; j <= tLen; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;

                int deletion = d[(i - 1) * stride + j] + 1;
                int insertion = d[i * stride + (j - 1)] + 1;
                int substitution = d[(i - 1) * stride + (j - 1)] + cost;

                int min = deletion < insertion ? deletion : insertion;
                if (substitution < min) min = substitution;

                if (i > 1 && j > 1
                    && source[i - 1] == target[j - 2]
                    && source[i - 2] == target[j - 1])
                {
                    int transposition = d[(i - 2) * stride + (j - 2)] + cost;
                    if (transposition < min) min = transposition;
                }

                d[i * stride + j] = min;
            }
        }

        return d[sLen * stride + tLen];
    }

    private static string ExtractInitials(ReadOnlySpan<char> target)
    {
        if (target.IsEmpty) return string.Empty;
        Span<char> buf = stackalloc char[target.Length];
        int count = 0;
        bool newWord = true;

        for (int i = 0; i < target.Length; i++)
        {
            char c = target[i];
            if (c is ' ' or '-' or '_' or '.' or '/' or '\\')
            {
                newWord = true;
                continue;
            }
            if (i > 0 && char.IsUpper(c) && char.IsLower(target[i - 1]))
                newWord = true;

            if (newWord)
            {
                buf[count++] = c;
                newWord = false;
            }
        }

        return new string(buf[..count]);
    }

    private static bool IsWordBoundary(int pos, ReadOnlySpan<char> target)
    {
        if (pos == 0) return true;
        char prev = target[pos - 1];
        return prev is ' ' or '-' or '_' or '.' or '/' or '\\';
    }
}
