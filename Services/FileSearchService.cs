namespace Arc.Services;

/// <summary>
/// Searches user-facing files and folders in Documents, Desktop, Downloads,
/// Pictures, Music, Videos, and OneDrive.
///
/// Only returns files with whitelisted extensions — no config files, logs,
/// system binaries, or development artifacts.  Directories are always ranked
/// ahead of files with the same fuzzy-match quality.
///
/// <see cref="MaxDepth"/> controls how deep the recursive search goes (1–5).
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

    // ── Directories to skip entirely ──────────────────────────────
    private static readonly HashSet<string> SkipDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "node_modules", ".git", "bin", "obj", "__pycache__", ".vs",
        "AppData", "Application Data", ".vscode", ".idea", ".nuget",
        "packages", "vendor", "bower_components",
    };

    // ── Whitelist: only these extensions appear in results ────────
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Documents
        ".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt", ".md",
        ".xls", ".xlsx", ".csv", ".ods",
        ".ppt", ".pptx", ".odp",
        ".epub", ".mobi",

        // Images
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico",
        ".tiff", ".tif", ".psd", ".ai", ".heic",

        // Audio
        ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a",

        // Video
        ".mp4", ".mov", ".avi", ".wmv", ".mkv", ".webm",

        // Archives
        ".zip", ".rar", ".7z", ".tar", ".gz",

        // Code / dev (users often search for their own source files)
        ".cs", ".py", ".js", ".jsx", ".ts", ".tsx", ".html", ".htm",
        ".css", ".scss", ".less", ".rs", ".go", ".java", ".kt",
        ".c", ".cpp", ".h", ".hpp", ".sh", ".ps1", ".sql", ".swift",
        ".rb", ".php", ".lua", ".r",

        // Web shortcuts
        ".url",

        // Notes
        ".one",
    };

    // ── File names to skip (version, license, readme) ─────────────
    private static readonly HashSet<string> SkipNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "version", "license", "licence", "readme", "changelog",
        "contributing", "authors", "notice", "thirdparty",
        "thumbs.db", "desktop.ini", ".ds_store",
        "appinfo", "release", "releases", "whatsnew", "history",
        "package", "package-lock", "yarn.lock", "composer",
        "makefile", "cmakelists", "dockerfile", ".gitignore",
        ".gitattributes", ".editorconfig",
    };

    /// <summary>Maximum folder depth for recursive search (1–5). Default 3.</summary>
    private static int _maxDepth = 3;
    public static int MaxDepth
    {
        get => _maxDepth;
        set => _maxDepth = Math.Clamp(value, 1, 5);
    }

    private const int MaxResults = 100;

    /// <summary>Searches for <paramref name="query"/> across user directories.</summary>
    public Task<List<SearchResult>> SearchAsync(string query, int maxReturn = 20, CancellationToken ct = default)
        => Task.Run(() => Search(query, maxReturn, ct), ct);

    /// <summary>Returns recently modified user files (for browse mode).</summary>
    public Task<List<SearchResult>> BrowseRecentAsync(int maxReturn = 50)
        => Task.Run(() => BrowseRecent(maxReturn));

    // ═══════════════════════════════════════════════════════════════
    // Browse (no query — show recent + folders first)
    // ═══════════════════════════════════════════════════════════════

    private List<SearchResult> BrowseRecent(int maxReturn)
    {
        var scored = new List<(double Score, SearchResult Result)>();

        foreach (var root in SearchRoots)
        {
            if (!Directory.Exists(root)) continue;
            CollectRecent(root, 0, scored);
            if (scored.Count >= MaxResults) break;
        }

        return scored
            .OrderByDescending(x => x.Score)
            .Take(maxReturn)
            .Select(x => x.Result)
            .ToList();
    }

    private static void CollectRecent(string dir, int depth,
        List<(double Score, SearchResult Result)> results)
    {
        if (depth > _maxDepth || results.Count >= MaxResults) return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                var info = new FileInfo(file);

                if ((info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                    continue;

                if (!IsUserFile(info)) continue;

                var days = (DateTime.Now - info.LastWriteTime).TotalDays;
                double score = Math.Max(0, 100 - days); // recency

                results.Add((score, new SearchResult
                {
                    Id            = $"file:{file}",
                    Type          = ResultType.File,
                    Name          = info.Name,
                    Subtitle      = info.LastWriteTime.ToString("MMM d, yyyy  h:mm tt")
                                    + "  •  " + (Path.GetDirectoryName(file) ?? ""),
                    IconPath      = file,
                    FilePath      = file,
                    FileExtension = info.Extension,
                    IsDirectory   = false,
                    Score         = score,
                }));

                if (results.Count >= MaxResults) return;
            }

            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                var dirName = Path.GetFileName(sub);
                if (SkipDirs.Contains(dirName)) continue;

                var dirInfo = new DirectoryInfo(sub);
                if ((dirInfo.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                    continue;

                // Folders get +1000 priority in browse mode too
                var days = (DateTime.Now - dirInfo.LastWriteTime).TotalDays;
                double score = 1000 + Math.Max(0, 100 - days);

                results.Add((score, new SearchResult
                {
                    Id          = $"file:{sub}",
                    Type        = ResultType.File,
                    Name        = dirName,
                    Subtitle    = Path.GetDirectoryName(sub) ?? "",
                    IconPath    = sub,
                    FilePath    = sub,
                    IsDirectory = true,
                    Score       = score,
                }));

                CollectRecent(sub, depth + 1, results);
                if (results.Count >= MaxResults) return;
            }
        }
        catch { /* skip inaccessible directories */ }
    }

    // ═══════════════════════════════════════════════════════════════
    // Search (with query — fuzzy match + ranking bonuses)
    // ═══════════════════════════════════════════════════════════════

    private List<SearchResult> Search(string query, int maxReturn, CancellationToken ct)
    {
        var results = new List<SearchResult>(MaxResults);
        if (string.IsNullOrWhiteSpace(query)) return results;

        foreach (var root in SearchRoots)
        {
            if (!Directory.Exists(root)) continue;
            if (ct.IsCancellationRequested) break;
            Recurse(root, query, 0, results, ct);
            if (results.Count >= MaxResults) break;
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(maxReturn)
            .ToList();
    }

    private static void Recurse(string dir, string query, int depth, List<SearchResult> results, CancellationToken ct)
    {
        if (ct.IsCancellationRequested || depth > _maxDepth || results.Count >= MaxResults) return;

        try
        {
            // ── Files ──────────────────────────────────────────
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                if (ct.IsCancellationRequested) return;
                var name = Path.GetFileName(file);
                var nameNoExt = Path.GetFileNameWithoutExtension(name);

                var info = new FileInfo(file);
                if ((info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                    continue;

                if (SkipNames.Contains(nameNoExt)) continue;
                if (!AllowedExtensions.Contains(info.Extension)) continue;

                double score = ScoreResult(query, name, isDirectory: false, info.LastWriteTime);
                if (score < 0) continue;

                results.Add(new SearchResult
                {
                    Id            = $"file:{file}",
                    Type          = ResultType.File,
                    Name          = name,
                    Subtitle      = Path.GetDirectoryName(file) ?? "",
                    IconPath      = file,
                    FilePath      = file,
                    FileExtension = info.Extension,
                    IsDirectory   = false,
                    Score         = score,
                });

                if (results.Count >= MaxResults) return;
            }

            // ── Directories ────────────────────────────────────
            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                if (ct.IsCancellationRequested) return;
                var dirName = Path.GetFileName(sub);
                if (SkipDirs.Contains(dirName)) continue;

                var dirInfo = new DirectoryInfo(sub);
                if ((dirInfo.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
                    continue;

                double score = ScoreResult(query, dirName, isDirectory: true, dirInfo.LastWriteTime);
                if (score >= 0)
                {
                    results.Add(new SearchResult
                    {
                        Id          = $"file:{sub}",
                        Type        = ResultType.File,
                        Name        = dirName,
                        Subtitle    = Path.GetDirectoryName(sub) ?? "",
                        IconPath    = sub,
                        FilePath    = sub,
                        IsDirectory = true,
                        Score       = score,
                    });

                    if (results.Count >= MaxResults) return;
                }

                Recurse(sub, query, depth + 1, results, ct);
                if (results.Count >= MaxResults) return;
            }
        }
        catch { /* skip inaccessible directories */ }
    }

    // ═══════════════════════════════════════════════════════════════
    // Scoring
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Computes a composite relevance score for a file or directory.
    /// Negative scores mean "no match" and the item is excluded.
    /// </summary>
    private static double ScoreResult(string query, string name,
        bool isDirectory, DateTime lastModified)
    {
        // Base fuzzy-match score (characters in order, rewards word starts)
        double score = FuzzySearch.Score(query, name);
        if (score < 0) return -1;

        // ── Bonuses (layered on top of fuzzy score) ────────────

        // Folders always rank ahead of identically-matched files
        if (isDirectory) score += 1000;

        // Exact name match (case-insensitive)
        if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase))
            score += 500;

        // Name starts with the query
        else if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            score += 200;

        // Recency: newer items score higher (max +100, decays over 100 days)
        double daysSinceModified = (DateTime.Now - lastModified).TotalDays;
        score += Math.Max(0, 100 - daysSinceModified);

        return score;
    }

    // ═══════════════════════════════════════════════════════════════
    // File filtering
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Returns true when the file has a whitelisted extension
    /// and is not a hidden/system/artifact file.</summary>
    private static bool IsUserFile(FileInfo info)
    {
        if ((info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0)
            return false;

        var name = info.Name;
        var ext  = info.Extension;

        var nameNoExt = Path.GetFileNameWithoutExtension(name);
        if (SkipNames.Contains(nameNoExt))
            return false;

        if (!AllowedExtensions.Contains(ext))
            return false;

        if (name.StartsWith(".", StringComparison.Ordinal))
            return false;

        return true;
    }
}

