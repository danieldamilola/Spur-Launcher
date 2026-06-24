namespace Spur.Services;

/// <summary>
/// Lightweight in-order fuzzy scorer. Characters must appear in the target
/// in the same order as the query. Rewards consecutive runs and word starts.
/// Returns -1 for no match, otherwise a score (higher = better match).
///
/// Also supports typo-tolerant matching via Damerau-Levenshtein distance
/// for short queries (3–10 characters). When the normal fuzzy match fails,
/// individual words in the target are checked for edit distance ≤ 2.
/// </summary>
public static class FuzzySearch
{
    /// <summary>
    /// Scores how well <paramref name="query"/> matches <paramref name="target"/>.
    /// Case-insensitive. Avoids allocations by comparing characters directly.
    /// </summary>
    public static double Score(string query, string target)
    {
        if (string.IsNullOrEmpty(query)) return 0;
        if (string.IsNullOrEmpty(target)) return -1;
        return ScoreCore(query.AsSpan(), target.AsSpan());
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

            if (char.ToLowerInvariant(qc) == char.ToLowerInvariant(tc))
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
        if (qi < query.Length)
        {
            // Typo-tolerance fallback: if no fuzzy match, try Damerau-Levenshtein
            // on individual words for short queries (3–10 chars).
            if (query.Length >= 3 && query.Length <= 10)
            {
                return ScoreTypoFallback(query, target);
            }
            return -1;
        }

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
    /// Typo-tolerance fallback: splits the target into words and checks
    /// Damerau-Levenshtein distance against the query. Returns a low positive
    /// score if a close match is found, or -1 if no match.
    /// </summary>
    private static double ScoreTypoFallback(ReadOnlySpan<char> query, ReadOnlySpan<char> target)
    {
        // Convert spans to strings for word splitting
        var queryStr = query.ToString();
        var targetStr = target.ToString();

        // Split target into words
        var separators = new[] { ' ', '-', '_', '.', '/', '\\' };
        var words = targetStr.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        int bestDistance = int.MaxValue;
        foreach (var word in words)
        {
            // Skip very short or very long words compared to query
            if (word.Length < query.Length - 2 || word.Length > query.Length + 2)
                continue;

            int distance = DamerauLevenshteinDistance(queryStr, word);
            if (distance < bestDistance)
                bestDistance = distance;
        }

        if (bestDistance <= 2)
        {
            // Low positive score so typo matches appear below exact matches.
            // Distance 1 → score 7, Distance 2 → score 4
            return 10.0 - bestDistance * 3.0;
        }

        return -1;
    }

    /// <summary>
    /// Computes the Damerau-Levenshtein distance between two strings.
    /// Supports insertions, deletions, substitutions, and transpositions.
    /// </summary>
    public static int DamerauLevenshteinDistance(string source, string target)
    {
        int sLen = source.Length;
        int tLen = target.Length;

        if (sLen == 0) return tLen;
        if (tLen == 0) return sLen;

        // Use a flat array instead of 2D for better cache performance
        var d = new int[(sLen + 1) * (tLen + 1)];
        int stride = tLen + 1;

        for (int i = 0; i <= sLen; i++) d[i * stride] = i;
        for (int j = 0; j <= tLen; j++) d[j] = j;

        for (int i = 1; i <= sLen; i++)
        {
            for (int j = 1; j <= tLen; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;

                int deletion     = d[(i - 1) * stride + j] + 1;
                int insertion    = d[i * stride + (j - 1)] + 1;
                int substitution = d[(i - 1) * stride + (j - 1)] + cost;

                int min = deletion < insertion ? deletion : insertion;
                if (substitution < min) min = substitution;

                // Transposition
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
