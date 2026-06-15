namespace Spur.Services;

/// <summary>
/// Lightweight in-order fuzzy scorer. Characters must appear in the target
/// in the same order as the query. Rewards consecutive runs and word starts.
/// Returns -1 for no match, otherwise a score (higher = better match).
/// </summary>
public static class FuzzySearch
{
    /// <summary>
    /// Scores how well <paramref name="query"/> matches <paramref name="target"/>.
    /// Uses pre-lowered strings to avoid per-character ToLowerInvariant overhead.
    /// </summary>
    public static double Score(string query, string target)
    {
        if (string.IsNullOrEmpty(query)) return 0;
        if (string.IsNullOrEmpty(target)) return -1;

        // Pre-lowercase once instead of per-character — saves significant
        // CPU when scoring thousands of candidates per keystroke.
        return ScoreCore(query.ToLowerInvariant(), target.ToLowerInvariant());
    }

    /// <summary>Span-based overload for advanced callers.</summary>
    public static double Score(ReadOnlySpan<char> query, ReadOnlySpan<char> target)
    {
        if (query.IsEmpty) return 0;
        if (target.IsEmpty) return -1;
        return ScoreCore(query, target);
    }

    private static double ScoreCore(ReadOnlySpan<char> query, ReadOnlySpan<char> target)
    {
        // --- Acronym / initials matching ---
        // If the query matches the first letters of each word in the target,
        // award a significant bonus. E.g. "vsc" matching "Visual Studio Code".
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
            if (prefixMatch)
            {
                acronymBonus = 50;
            }
        }

        int qi = 0, ti = 0;
        int consecutive = 0;
        double score = 0;
        bool isFirstMatch = true; // only give word-start bonus to the first matched char

        while (qi < query.Length && ti < target.Length)
        {
            char qc = query[qi];
            char tc = target[ti];

            if (qc == tc)
            {
                consecutive++;

                // Word-start bonus: only on the first matched character.
                // Previously this was applied at every separator, which caused
                // "a_bc" to score higher than a perfect "abc" match because
                // the separator bonus (+0.8) outweighed the coverage penalty.
                if (isFirstMatch && IsWordBoundary(ti, target))
                {
                    score += 1.0;
                    isFirstMatch = false;
                }

                // Consecutive match bonus (diminishing)
                score += 0.1 * consecutive;

                // Exact case bonus (only meaningful for the string overload
                // where the caller may pass original casing; for pre-lowered
                // strings this is always 0)
                qi++;
            }
            else
            {
                consecutive = 0;
            }

            ti++;
        }

        // All query chars must be matched
        if (qi < query.Length) return -1;

        // Prefer shorter targets (tighter match)
        double coverageBonus = (double)query.Length / target.Length;
        return score + coverageBonus + acronymBonus;
    }

    /// <summary>
    /// Extracts the initials (first letter of each word) from the target string.
    /// Words are delimited by spaces, hyphens, underscores, dots, slashes, and
    /// camelCase transitions (lowercase → uppercase).
    /// </summary>
    private static string ExtractInitials(ReadOnlySpan<char> target)
    {
        if (target.IsEmpty) return string.Empty;

        // Use a stack-allocated buffer for typical short initial sequences
        Span<char> buf = stackalloc char[target.Length];
        int count = 0;

        bool newWord = true;
        for (int i = 0; i < target.Length; i++)
        {
            char c = target[i];

            // Separator characters start a new word
            if (c is ' ' or '-' or '_' or '.' or '/' or '\\')
            {
                newWord = true;
                continue;
            }

            // CamelCase boundary: lowercase followed by uppercase
            if (i > 0 && char.IsUpper(c) && char.IsLower(target[i - 1]))
            {
                newWord = true;
            }

            if (newWord)
            {
                buf[count++] = c;
                newWord = false;
            }
        }

        return new string(buf[..count]);
    }

    /// <summary>
    /// Checks if position <paramref name="pos"/> in <paramref name="target"/>
    /// is at a word boundary (start of string or preceded by a separator).
    /// </summary>
    private static bool IsWordBoundary(int pos, ReadOnlySpan<char> target)
    {
        if (pos == 0) return true;
        char prev = target[pos - 1];
        return prev is ' ' or '-' or '_' or '.' or '/' or '\\';
    }
}
