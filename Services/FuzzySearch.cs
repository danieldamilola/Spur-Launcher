namespace Volt.Services;

/// <summary>
/// Lightweight in-order fuzzy scorer. Characters must appear in the target
/// in the same order as the query. Rewards consecutive runs and word starts.
/// Returns -1 for no match, otherwise a score ≥ 0 (higher = better match).
/// </summary>
public static class FuzzySearch
{
    /// <summary>
    /// Scores how well <paramref name="query"/> matches <paramref name="target"/>.
    /// </summary>
    public static double Score(ReadOnlySpan<char> query, ReadOnlySpan<char> target)
    {
        if (query.IsEmpty) return 0;
        if (target.IsEmpty) return -1;

        int qi = 0, ti = 0;
        int consecutive = 0;
        double score = 0;
        bool prevWasSep = true; // treat start of string as a word boundary

        while (qi < query.Length && ti < target.Length)
        {
            char qc = char.ToLowerInvariant(query[qi]);
            char tc = char.ToLowerInvariant(target[ti]);

            if (qc == tc)
            {
                consecutive++;

                // Word-start bonus
                if (prevWasSep) score += 0.8;

                // Consecutive match bonus (diminishing)
                score += 0.1 * consecutive;

                // Exact case bonus
                if (query[qi] == target[ti]) score += 0.1;

                qi++;
            }
            else
            {
                consecutive = 0;
            }

            prevWasSep = tc is ' ' or '-' or '_' or '.' or '/';
            ti++;
        }

        // All query chars must be matched
        if (qi < query.Length) return -1;

        // Prefer shorter targets (tighter match)
        double coverageBonus = (double)query.Length / target.Length;
        return score + coverageBonus;
    }

    /// <summary>Scores and returns -1 if query is empty or no match.</summary>
    public static double Score(string query, string target)
        => string.IsNullOrEmpty(query) ? 0 : Score(query.AsSpan(), target.AsSpan());
}
