namespace Volt.Services;

/// <summary>
/// Searches files and folders in common user directories up to 3 levels deep.
/// Prioritises recent and frequently accessed file types.
/// </summary>
public sealed class FileSearchService
{
    private static readonly string[] SearchRoots =
    [
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive"),
    ];

    private static readonly HashSet<string> SkipDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules", ".git", "bin", "obj", "__pycache__", ".vs",
        "AppData", "Application Data",
    };

    private const int MaxDepth   = 3;
    private const int MaxResults = 200; // internal pool before fuzzy ranking

    /// <summary>Searches for <paramref name="query"/> across file system roots.</summary>
    public Task<List<SearchResult>> SearchAsync(string query, int maxReturn = 20)
        => Task.Run(() => Search(query, maxReturn));

    private List<SearchResult> Search(string query, int maxReturn)
    {
        var results = new List<SearchResult>(MaxResults);
        if (string.IsNullOrWhiteSpace(query)) return results;

        foreach (var root in SearchRoots)
        {
            if (!Directory.Exists(root)) continue;
            Recurse(root, query, 0, results);
            if (results.Count >= MaxResults) break;
        }

        // Score and rank
        return results
            .Select(r => { r.Score = FuzzySearch.Score(query, r.Name); return r; })
            .Where(r => r.Score >= 0)
            .OrderByDescending(r => r.Score)
            .Take(maxReturn)
            .ToList();
    }

    private static void Recurse(string dir, string query, int depth, List<SearchResult> results)
    {
        if (depth > MaxDepth || results.Count >= MaxResults) return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                var name = Path.GetFileName(file);
                if (FuzzySearch.Score(query, name) < 0) continue;

                var info = new FileInfo(file);
                results.Add(new SearchResult
                {
                    Id           = $"file:{file}",
                    Type         = ResultType.File,
                    Name         = name,
                    Subtitle     = Path.GetDirectoryName(file) ?? "",
                    IconPath     = file,
                    FilePath     = file,
                    FileExtension = info.Extension,
                });

                if (results.Count >= MaxResults) return;
            }

            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                var dirName = Path.GetFileName(sub);
                if (SkipDirs.Contains(dirName)) continue;

                if (FuzzySearch.Score(query, dirName) >= 0)
                {
                    results.Add(new SearchResult
                    {
                        Id       = $"file:{sub}",
                        Type     = ResultType.File,
                        Name     = dirName,
                        Subtitle = Path.GetDirectoryName(sub) ?? "",
                        IconPath = sub,
                        FilePath = sub,
                    });
                }

                Recurse(sub, query, depth + 1, results);
                if (results.Count >= MaxResults) return;
            }
        }
        catch { /* skip inaccessible directories */ }
    }
}
