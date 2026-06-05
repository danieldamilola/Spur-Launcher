namespace Spur.Services;

/// <summary>Interface for file/folder search.</summary>
public interface IFileSearchService
{
    /// <summary>Maximum folder depth for recursive search (1–5). Default 3.</summary>
    int MaxDepth { get; set; }

    /// <summary>Searches for <paramref name="query"/> across user directories.</summary>
    Task<List<SearchResult>> SearchAsync(string query, int maxReturn = 20, CancellationToken ct = default);

    /// <summary>Returns recently modified user files (for browse mode).</summary>
    Task<List<SearchResult>> BrowseRecentAsync(int maxReturn = 50);
}

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
public sealed class FileSearchService : IFileSearchService
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

    private int _maxDepth = 3;
    public int MaxDepth
    {
        get => _maxDepth;
        set => _maxDepth = Math.Clamp(value, 1, 5);
    }

    private const int MaxResults = 100;
    
    // ── In-Memory Cache ───────────────────────────────────────────
    private readonly object _cacheLock = new();
    private List<CachedItem> _cache = [];
    private bool _isIndexing = false;
    private DateTime _lastIndexTime = DateTime.MinValue;

    private struct CachedItem
    {
        public string Path;
        public string Name;
        public string NameNoExt;
        public string Ext;
        public string DirName;
        public bool IsDirectory;
        public DateTime LastWriteTime;
    }

    public FileSearchService()
    {
        // Kick off background indexing on startup
        Task.Run(BuildIndexAsync);
    }

    private async Task BuildIndexAsync()
    {
        if (_isIndexing) return;
        lock (_cacheLock) { _isIndexing = true; }

        try
        {
            var newCache = new List<CachedItem>(5_000);
            foreach (var root in SearchRoots)
            {
                if (!Directory.Exists(root)) continue;
                await Task.Run(() => IndexDirectory(root, 0, newCache));
            }

            lock (_cacheLock)
            {
                _cache = newCache;
                _lastIndexTime = DateTime.Now;
            }

            // Release Gen2 memory after the one-time startup index build
            GC.Collect(2, GCCollectionMode.Optimized, false);
        }
        catch { /* ignore background index errors */ }
        finally
        {
            lock (_cacheLock) { _isIndexing = false; }
        }
    }

    private void IndexDirectory(string dir, int depth, List<CachedItem> results)
    {
        if (depth > MaxDepth || results.Count >= 20_000) return; // Cap at 20k items to save RAM

        try
        {
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                var info = new FileInfo(file);
                if ((info.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue;
                if (!IsUserFile(info)) continue;

                results.Add(new CachedItem
                {
                    Path = file,
                    Name = info.Name,
                    NameNoExt = Path.GetFileNameWithoutExtension(info.Name),
                    Ext = info.Extension,
                    DirName = Path.GetDirectoryName(file) ?? "",
                    IsDirectory = false,
                    LastWriteTime = info.LastWriteTime
                });
            }

            foreach (var sub in Directory.EnumerateDirectories(dir))
            {
                var dirName = Path.GetFileName(sub);
                if (SkipDirs.Contains(dirName)) continue;

                var dirInfo = new DirectoryInfo(sub);
                if ((dirInfo.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue;

                results.Add(new CachedItem
                {
                    Path = sub,
                    Name = dirName,
                    NameNoExt = dirName,
                    Ext = "",
                    DirName = Path.GetDirectoryName(sub) ?? "",
                    IsDirectory = true,
                    LastWriteTime = dirInfo.LastWriteTime
                });

                IndexDirectory(sub, depth + 1, results);
            }
        }
        catch { }
    }

    /// <summary>Searches for <paramref name="query"/> across user directories.</summary>
    public Task<List<SearchResult>> SearchAsync(string query, int maxReturn = 20, CancellationToken ct = default)
        => Task.Run(() => SearchCached(query, maxReturn, ct), ct);

    /// <summary>Returns recently modified user files (for browse mode).</summary>
    public Task<List<SearchResult>> BrowseRecentAsync(int maxReturn = 50)
        => Task.Run(() => BrowseRecentCached(maxReturn));

    // ═══════════════════════════════════════════════════════════════
    // Browse (no query — show recent + folders first)
    // ═══════════════════════════════════════════════════════════════

    private List<SearchResult> BrowseRecentCached(int maxReturn)
    {
        // Refresh index if it's older than 30 mins
        if ((DateTime.Now - _lastIndexTime).TotalMinutes > 30)
            Helpers.SafeFireAndForget.Run(BuildIndexAsync, null, "BuildIndex");

        List<CachedItem> localCache;
        lock (_cacheLock) { localCache = _cache; }

        var results = new List<(double Score, SearchResult Result)>(MaxResults);

        // Sort descending by LastWriteTime, take top 100 to score
        var recentItems = localCache
            .OrderByDescending(x => x.LastWriteTime)
            .Take(200);

        foreach (var item in recentItems)
        {
            var days = (DateTime.Now - item.LastWriteTime).TotalDays;
            double score = Math.Max(0, 100 - days);
            if (item.IsDirectory) score += 1000;

            results.Add((score, new SearchResult
            {
                Id            = $"file:{item.Path}",
                Type          = ResultType.File,
                Name          = item.Name,
                Subtitle      = item.IsDirectory ? item.DirName : (item.LastWriteTime.ToString("MMM d, yyyy  h:mm tt") + "  •  " + item.DirName),
                IconPath      = item.Path,
                FilePath      = item.Path,
                FileExtension = item.Ext,
                IsDirectory   = item.IsDirectory,
                Score         = score,
            }));
        }

        results.Sort((a, b) => b.Score.CompareTo(a.Score));
        var final = new List<SearchResult>(maxReturn);
        for (int i = 0; i < results.Count && i < maxReturn; i++)
            final.Add(results[i].Result);
        return final;
    }

    // ═══════════════════════════════════════════════════════════════
    // Search (with query — fuzzy match + ranking bonuses)
    // ═══════════════════════════════════════════════════════════════

    private List<SearchResult> SearchCached(string query, int maxReturn, CancellationToken ct)
    {
        var results = new List<SearchResult>(MaxResults);
        if (string.IsNullOrWhiteSpace(query)) return results;

        List<CachedItem> localCache;
        lock (_cacheLock) { localCache = _cache; }

        // Refresh index if it's empty or very old
        if (localCache.Count == 0 || (DateTime.Now - _lastIndexTime).TotalMinutes > 30)
            Helpers.SafeFireAndForget.Run(BuildIndexAsync, null, "BuildIndex");

        var queryLower = query.ToLowerInvariant();
        
        foreach (var item in localCache)
        {
            if (ct.IsCancellationRequested) break;
            
            // Fast prefix check
            if (!item.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                // Try fuzzy if contains fails, but skip fuzzy on massive lists unless it's a good candidate to save CPU
                var fuzzy = FuzzySearch.Score(query, item.Name);
                if (fuzzy < 10) continue;
            }

            double score = ScoreResult(query, item.Name, item.IsDirectory, item.LastWriteTime);
            if (score < 0) continue;

            results.Add(new SearchResult
            {
                Id            = $"file:{item.Path}",
                Type          = ResultType.File,
                Name          = item.Name,
                Subtitle      = item.DirName,
                IconPath      = item.Path,
                FilePath      = item.Path,
                FileExtension = item.Ext,
                IsDirectory   = item.IsDirectory,
                Score         = score,
            });
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(maxReturn)
            .ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // Scoring
    // ═══════════════════════════════════════════════════════════════

    private static double ScoreResult(string query, string name, bool isDirectory, DateTime lastModified)
    {
        double score = FuzzySearch.Score(query, name);
        if (score < 0) return -1;

        if (isDirectory) score += 1000;
        if (string.Equals(name, query, StringComparison.OrdinalIgnoreCase)) score += 500;
        else if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 200;

        double daysSinceModified = (DateTime.Now - lastModified).TotalDays;
        score += Math.Max(0, 100 - daysSinceModified);

        return score;
    }

    // ═══════════════════════════════════════════════════════════════
    // File filtering
    // ═══════════════════════════════════════════════════════════════

    private static bool IsUserFile(FileInfo info)
    {
        var nameNoExt = Path.GetFileNameWithoutExtension(info.Name);
        if (SkipNames.Contains(nameNoExt)) return false;
        if (!AllowedExtensions.Contains(info.Extension)) return false;
        if (info.Name.StartsWith(".", StringComparison.Ordinal)) return false;
        return true;
    }
}
