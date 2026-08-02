using Spur.Services.FileSearch;

namespace Spur.Services;

/// <summary>Interface for file/folder search.</summary>
public interface IFileSearchService
{
    /// <summary>Maximum folder depth for recursive search (1-5). Default 3.</summary>
    int MaxDepth { get; set; }

    /// <summary>Searches for <paramref name="query"/> across user directories.</summary>
    Task<List<SearchResult>> SearchAsync(string query, int maxReturn = 20, CancellationToken ct = default);

    /// <summary>Returns recently modified user files (for browse mode).</summary>
    Task<List<SearchResult>> BrowseRecentAsync(int maxReturn = 50);

    /// <summary>Lists files/folders inside a directory. Returns null if inaccessible.</summary>
    List<SearchResult>? ListDirectory(string dirPath, string? filter = null);

    /// <summary>Resolves the first path segment to a matching root directory.</summary>
    string? ResolveRootSegment(string segment);

    /// <summary>Returns the root search directories (Desktop, Documents, etc.).</summary>
    string[] GetRootDirectories();
}

/// <summary>
/// Searches user-facing files and folders in Documents, Desktop, Downloads,
/// Pictures, Music, Videos, and OneDrive.
///
/// Only returns files with whitelisted extensions — no config files, logs,
/// system binaries, or development artifacts.  Directories are always ranked
/// ahead of files with the same fuzzy-match quality.
///
/// <see cref="MaxDepth"/> controls how deep the recursive search goes (1-5).
/// </summary>
public sealed class FileSearchService : IFileSearchService
{
    // ── Result ID and Type Constants ──────────────────────────────
    private const string FileIdPrefix = "file:";

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

    /// <summary>Maximum number of scored results before final truncation.</summary>
    private const int MaxSearchResults = 100;
    /// <summary>Hard cap on total cached items to bound memory usage.</summary>
    private const int MaxCacheSize = 20_000;

    // ── In-Memory Cache ───────────────────────────────────────────
    private readonly object _cacheLock = new();
    private List<CachedItem> _cache = [];
    private bool _isIndexing = false;
    private DateTime _lastIndexTime = DateTime.MinValue;
    private volatile bool _isReady;

    /// <summary>True once the initial background index build has completed at least once.</summary>
    public bool IsReady => _isReady;

    /// <summary>
    /// Guards against infinite re-index loops: if BuildIndexAsync fails and the
    /// cache stays empty, SearchCached would re-trigger on every keystroke.
    /// This cooldown prevents re-triggering within N seconds of a failed attempt.
    /// </summary>
    private DateTime _lastIndexAttempt = DateTime.MinValue;
    private const int ReIndexCooldownSeconds = 60;

    // Local string pool — deduplicates directory paths per indexing cycle
    // without polluting the global CLR intern pool. Cleared at the start
    // of each BuildIndexAsync so stale paths don't accumulate.
    private readonly Dictionary<string, string> _stringPool = new(StringComparer.Ordinal);

    private string PoolString(string s)
    {
        if (_stringPool.TryGetValue(s, out var cached)) return cached;
        _stringPool[s] = s;
        return s;
    }

    private struct CachedItem
    {
        public string Path;
        public string Name;
        public string NameLower;
        public string NameNoExt;
        public string Ext;
        public string DirName;
        public bool IsDirectory;
        public DateTime LastWriteTime;
    }

    private readonly SpurConfig _config;
    private readonly ILogger? _log;

    public FileSearchService(SpurConfig config, ILogger? log = null)
    {
        _config = config;
        _log = log;
        // Kick off background indexing on startup
        Task.Run(BuildIndexAsync);
    }

    private async Task BuildIndexAsync()
    {
        if (_isIndexing) return;
        lock (_cacheLock) { _isIndexing = true; }

        try
        {
            _lastIndexAttempt = DateTime.Now;
            _stringPool.Clear();

            var newCache = new List<CachedItem>(5_000);
            foreach (var root in SearchRoots)
            {
                if (!Directory.Exists(root)) continue;
                await IndexDirectoryAsync(root, 0, newCache);
            }

            lock (_cacheLock)
            {
                _cache = newCache;
                _lastIndexTime = DateTime.Now;
            }

            _isReady = true;
        }
        catch (Exception ex)
        {
            _log?.Warning("Background index build failed", ex);
        }
        finally
        {
            lock (_cacheLock) { _isIndexing = false; }
        }
    }

    private async Task IndexDirectoryAsync(string dir, int depth, List<CachedItem> results)
    {
        if (depth > MaxDepth || results.Count >= MaxCacheSize) return;

        // Yield to the scheduler to prevent CPU pinning during large enumerations.
        // Unlike Task.Delay(1), this adds zero artificial latency.
        await Task.Yield();

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
                    NameLower = info.Name.ToLowerInvariant(),
                    NameNoExt = Path.GetFileNameWithoutExtension(info.Name),
                    Ext = PoolString(info.Extension.ToLowerInvariant()),
                    DirName = PoolString(Path.GetDirectoryName(file) ?? ""),
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
                    NameLower = dirName.ToLowerInvariant(),
                    NameNoExt = dirName,
                    Ext = "",
                    DirName = PoolString(Path.GetDirectoryName(sub) ?? ""),
                    IsDirectory = true,
                    LastWriteTime = dirInfo.LastWriteTime
                });

                await IndexDirectoryAsync(sub, depth + 1, results);
            }
        }
        catch (UnauthorizedAccessException) { /* skip inaccessible directories */ }
        catch (PathTooLongException) { /* skip deeply nested paths */ }
        catch (Exception ex) { _log?.Debug($"Skipped dir {dir}: {ex.Message}"); }
    }

    /// <summary>Searches for <paramref name="query"/> across user directories.</summary>
    public async Task<List<SearchResult>> SearchAsync(string query, int maxReturn = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        // Try Everything first (fastest)
        if (EverythingProvider.IsAvailable)
        {
            var everythingPaths = await EverythingProvider.SearchAsync(query, maxReturn, ct);
            if (everythingPaths.Count > 0)
            {
                var results = new List<SearchResult>(everythingPaths.Count);
                foreach (var path in everythingPaths)
                {
                    if (ct.IsCancellationRequested) break;
                    var isDir = Directory.Exists(path);
                    var name = Path.GetFileName(path);
                    results.Add(new SearchResult
                    {
                        Id = $"{FileIdPrefix}{path}",
                        Type = ResultType.File,
                        Name = name,
                        Subtitle = Path.GetDirectoryName(path) ?? "",
                        IconPath = path,
                        FilePath = path,
                        FileExtension = Path.GetExtension(path),
                        IsDirectory = isDir,
                        Score = 1000,
                    });
                }
                return results;
            }
        }

        // Try Windows Search Index next
        if (WindowsIndexProvider.IsAvailable)
        {
            var wsiResults = await WindowsIndexProvider.SearchAsync(query, maxReturn, ct);
            if (wsiResults.Count > 0)
            {
                var results = new List<SearchResult>(wsiResults.Count);
                foreach (var (path, isFolder) in wsiResults)
                {
                    if (ct.IsCancellationRequested) break;
                    if (!File.Exists(path) && !Directory.Exists(path)) continue;
                    results.Add(new SearchResult
                    {
                        Id = $"{FileIdPrefix}{path}",
                        Type = ResultType.File,
                        Name = Path.GetFileName(path),
                        Subtitle = Path.GetDirectoryName(path) ?? "",
                        IconPath = path,
                        FilePath = path,
                        FileExtension = Path.GetExtension(path),
                        IsDirectory = isFolder,
                        Score = 1000,
                    });
                }
                if (results.Count > 0) return results;
            }
        }

        // Fall back to cached file system walk
        return await Task.Run(() => SearchCached(query, maxReturn, ct), ct);
    }

    /// <summary>Returns recently modified user files (for browse mode).</summary>
    public Task<List<SearchResult>> BrowseRecentAsync(int maxReturn = 50)
        => Task.Run(() => BrowseRecentCached(maxReturn));

    // ═══════════════════════════════════════════════════════════════
    // Browse (no query — show recent + folders first)
    // ═══════════════════════════════════════════════════════════════

    private List<SearchResult> BrowseRecentCached(int maxReturn)
    {
        // Refresh index if it's older than config interval
        var intervalMins = _config.ReIndexIntervalHours * 60;
        if (intervalMins > 0 && (DateTime.Now - _lastIndexTime).TotalMinutes > intervalMins)
            Helpers.SafeFireAndForget.Run(BuildIndexAsync, _log, "BuildIndex");

        List<CachedItem> localCache;
        lock (_cacheLock) { localCache = _cache; }

        var results = new List<(double Score, SearchResult Result)>(MaxSearchResults);

        // Sort descending by LastWriteTime, take top 200 to score
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
                Id = $"{FileIdPrefix}{item.Path}",
                Type = ResultType.File,
                Name = item.Name,
                Subtitle = item.IsDirectory
                    ? item.DirName
                    : (item.LastWriteTime.ToString("MMM d, yyyy  h:mm tt") + "  \u2022  " + item.DirName),
                IconPath = item.Path,
                FilePath = item.Path,
                FileExtension = item.Ext,
                IsDirectory = item.IsDirectory,
                Score = score,
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
        var results = new List<SearchResult>(MaxSearchResults);
        if (string.IsNullOrWhiteSpace(query)) return results;

        List<CachedItem> localCache;
        lock (_cacheLock) { localCache = _cache; }

        var intervalMins = _config.ReIndexIntervalHours * 60;
        // Refresh index if it's empty or very old — but respect cooldown to
        // prevent infinite re-index loops after a failed build.
        bool cacheEmpty = localCache.Count == 0;
        bool cacheStale = intervalMins > 0 && (DateTime.Now - _lastIndexTime).TotalMinutes > intervalMins;
        bool recentlyFailed = (DateTime.Now - _lastIndexAttempt).TotalSeconds < ReIndexCooldownSeconds;

        if ((cacheEmpty && !recentlyFailed) || (cacheStale && !cacheEmpty))
            Helpers.SafeFireAndForget.Run(BuildIndexAsync, _log, "BuildIndex");

        // Lower the query once — all comparisons use pre-lowered data
        var queryLower = query.ToLowerInvariant();

        foreach (var item in localCache)
        {
            if (ct.IsCancellationRequested) break;

            if (!item.NameLower.Contains(queryLower, StringComparison.Ordinal))
            {
                if (Math.Abs(item.Name.Length - query.Length) > 15) continue;

                var pre = FuzzySearch.Score(queryLower.AsSpan(), item.NameLower.AsSpan());
                if (pre < 20) continue;
            }

            var match = FuzzySearch.Match(query, item.Name);
            if (!match.Success) continue;

            double score = match.Score;
            if (item.IsDirectory) score += 1000;
            if (string.Equals(item.Name, query, StringComparison.OrdinalIgnoreCase)) score += 500;
            else if (item.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 200;

            double daysSinceModified = (DateTime.Now - item.LastWriteTime).TotalDays;
            score += Math.Max(0, 100 - daysSinceModified);

            results.Add(new SearchResult
            {
                Id = $"{FileIdPrefix}{item.Path}",
                Type = ResultType.File,
                Name = item.Name,
                Subtitle = item.DirName,
                IconPath = item.Path,
                FilePath = item.Path,
                FileExtension = item.Ext,
                IsDirectory = item.IsDirectory,
                Score = score,
                TitleHighlightData = match.MatchedIndices as HashSet<int>,
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



    // ═══════════════════════════════════════════════════════════════
    // Folder navigation
    // ═══════════════════════════════════════════════════════════════

    public List<SearchResult>? ListDirectory(string dirPath, string? filter = null)
    {
        if (!Directory.Exists(dirPath)) return null;

        var results = new List<SearchResult>();
        try
        {
            var entries = Directory.EnumerateFileSystemEntries(dirPath).ToList();
            entries.Sort((a, b) =>
            {
                bool aIsDir = Directory.Exists(a);
                bool bIsDir = Directory.Exists(b);
                if (aIsDir != bIsDir) return aIsDir ? -1 : 1;
                return string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase);
            });

            foreach (var entry in entries)
            {
                var name = Path.GetFileName(entry);
                if (SkipNames.Contains(name)) continue;
                if (name.StartsWith(".", StringComparison.Ordinal)) continue;
                var attrs = File.GetAttributes(entry);
                if ((attrs & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue;

                bool isDir = (attrs & FileAttributes.Directory) == FileAttributes.Directory;

                if (isDir)
                {
                    if (SkipDirs.Contains(name)) continue;
                }
                else
                {
                    var ext = Path.GetExtension(entry);
                    if (!AllowedExtensions.Contains(ext)) continue;
                }

                if (filter is not null && !name.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;

                results.Add(new SearchResult
                {
                    Id = $"{FileIdPrefix}{entry}",
                    Type = ResultType.File,
                    Name = name,
                    Subtitle = dirPath,
                    IconPath = entry,
                    FilePath = entry,
                    FileExtension = Path.GetExtension(entry),
                    IsDirectory = isDir,
                    Score = isDir ? 1000 : 0,
                });
            }
        }
        catch { return null; }

        return results;
    }

    public string? ResolveRootSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment)) return null;

        // Try exact match first, then prefix match against root directory names
        string? bestMatch = null;
        foreach (var root in SearchRoots)
        {
            if (!Directory.Exists(root)) continue;
            var dirName = Path.GetFileName(root);

            if (dirName.Equals(segment, StringComparison.OrdinalIgnoreCase))
                return root;

            if (dirName.StartsWith(segment, StringComparison.OrdinalIgnoreCase))
                bestMatch ??= root;
        }
        return bestMatch;
    }

    public string[] GetRootDirectories() => SearchRoots;

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
