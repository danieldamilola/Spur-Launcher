using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Arc.Services;

namespace Arc.Services;

/// <summary>
/// Discovers user-facing installed applications exclusively from Windows Start Menu
/// .lnk shortcuts. This is the canonical list of everything a user would ever click
/// to launch — classic desktop apps, Windows system tools, and modern UWP apps.
///
/// No registry scanning, no PATH enumeration, no COM shell folder enumeration.
/// Those sources add hundreds of system executables, CLI tools, background
/// processes, and framework packages that users never need to see.
/// </summary>
public interface IAppDiscoveryService
{
    Task<List<SearchResult>> DiscoverAsync(CancellationToken ct = default);
    void ClearCache();
}

public sealed class AppDiscoveryService : IAppDiscoveryService
{
    private readonly ILogger _logger;

    public AppDiscoveryService(ILogger logger)
    {
        _logger = logger;
    }
    private static readonly string[] StartMenuPaths =
    [
        Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
    ];

    /// <summary>Top-level Desktop .lnk shortcuts (non-recursive).</summary>
    private static readonly string DesktopPath =
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    private static readonly string[] StartUpPaths =
    [
        Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
    ];

    // ── Uninstaller file names (exact match) ──────────────────────
    private static readonly HashSet<string> UninstallerExes =
        new(StringComparer.OrdinalIgnoreCase)
    {
        "uninst.exe", "unins000.exe", "uninst000.exe", "uninstall.exe",
        "unwise.exe", "unwise32.exe",
    };

    // ── Shortcut names that are never real apps ───────────────────
    private static readonly HashSet<string> JunkNames =
        new(StringComparer.OrdinalIgnoreCase)
    {
        "uninstall", "help", "readme", "license", "licence",
        "changelog", "what's new", "getting started", "user guide",
        "documentation", "user manual", "quick start", "tutorial",
        "website", "home page", "online support", "visit our website",
        "support", "technical support", "customer support",
        "diagnostics", "repair", "configure", "configuration",
        "registration", "register", "activate",
        "update", "check for updates", "upgrade",
        "release notes", "known issues", "troubleshooting",
        "order", "buy now", "purchase",
        "send feedback", "rate this app", "survey",
    };

    // ── .lnk target executables that are never user apps ──────────
    private static readonly HashSet<string> NonAppTargets =
        new(StringComparer.OrdinalIgnoreCase)
    {
        "rundll32.exe", "control.exe", "mmc.exe", "regedit.exe",
        "cmd.exe", "powershell.exe", "pwsh.exe", "wscript.exe",
        "cscript.exe", "mshta.exe", "msiexec.exe", "taskmgr.exe",
        "explorer.exe",
        "dxdiag.exe", "winver.exe", "charmap.exe",
        "cleanmgr.exe", "diskpart.exe", "diskmgmt.msc",
        "services.msc", "devmgmt.msc", "eventvwr.msc",
        "perfmon.msc", "taskschd.msc", "gpedit.msc",
        "secpol.msc", "compmgmt.msc", "wf.msc",
    };

    // ── Well-known Windows apps to keep (others in System32/WindowsApps are blocked) ──
    private static readonly HashSet<string> AllowedWindowsApps =
        new(StringComparer.OrdinalIgnoreCase)
    {
        "Snipping Tool", "Notepad", "Settings", "Windows Security", "Windows Defender",
        "Calculator", "File Explorer", "Windows Terminal", "Terminal",
        "PowerShell", "PowerShell 7", "Paint", "WordPad",
        "Command Prompt", "Control Panel",
    };

    // ── Cache ─────────────────────────────────────────────────────
    private static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Arc", "Arc.catalog.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    // ── COM IShellLink ────────────────────────────────────────────
    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink { }

    [ComImport, Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
            int cchMaxPath, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
            int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport, Guid("0000010B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    // ══════════════════════════════════════════════════════════════
    // Public
    // ══════════════════════════════════════════════════════════════

    public Task<List<SearchResult>> DiscoverAsync(CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<List<SearchResult>>();
        var thread = new Thread(() =>
        {
            try
            {
                if (ct.IsCancellationRequested)
                {
                    tcs.SetCanceled();
                    return;
                }
                var cached = LoadCache();
                if (cached is { Count: > 0 })
                {
                    tcs.SetResult(cached);
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try { SaveCache(DiscoverAll()); }
                        catch { }
                    });
                }
                else
                {
                    var fresh = DiscoverAll();
                    SaveCache(fresh);
                    tcs.SetResult(fresh);
                }
            }
            catch (Exception ex) { tcs.SetException(ex); }
        })
        { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }

    // ── .lnk resolve result ───────────────────────────────────────
    private sealed record LnkInfo(string TargetPath, bool IsUwp);

    // ══════════════════════════════════════════════════════════════
    // Discovery
    // ══════════════════════════════════════════════════════════════

    private List<SearchResult> DiscoverAll()
    {
        var results = new List<SearchResult>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in StartMenuPaths)
        {
            if (!Directory.Exists(root)) continue;

            foreach (var lnk in Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories))
            {
                try
                {
                    var info = ResolveLnk(lnk);
                    var exePath = info.TargetPath;
                    if (string.IsNullOrEmpty(exePath)) continue;

                    var name = Path.GetFileNameWithoutExtension(lnk);
                    var folder = Path.GetFileName(Path.GetDirectoryName(lnk)) ?? "";

                    // ── Aggressive filtering ──────────────────────

                    // Skip junk shortcut names
                    if (JunkNames.Contains(name)) continue;

                    // Skip uninstallers by exe filename
                    var exeFile = Path.GetFileName(exePath);
                    if (UninstallerExes.Contains(exeFile)) continue;

                    // Skip uninstaller prefixes (multi-language)
                    if (IsUninstallerByName(name)) continue;

                    // Skip startup entries
                    if (IsStartupEntry(lnk)) continue;

                    // Skip shortcuts that resolve to non-user-app launchers.
                    // UWP apps launch via explorer.exe — allow those through.
                    if (!info.IsUwp && NonAppTargets.Contains(exeFile)) continue;

                    // Skip anything in a Documentation, Help, or Manuals folder
                    if (folder.Contains("ocument", StringComparison.OrdinalIgnoreCase) ||
                        folder.Contains("help", StringComparison.OrdinalIgnoreCase) ||
                        folder.Contains("manual", StringComparison.OrdinalIgnoreCase) ||
                        folder.StartsWith("—", StringComparison.Ordinal)) continue;

                    var ext = Path.GetExtension(exePath);
                    if (!string.IsNullOrEmpty(ext))
                    {
                        var extLower = ext.ToLowerInvariant();
                        if (extLower is not (".exe" or ".lnk" or ".msc" or ".bat" or ".cmd" or ".com"))
                        {
                            continue;
                        }
                    }

                    // Filter Windows/System clutter.
                    // UWP apps live under WindowsApps — allow those through.
                    // Allow well-known Windows apps.
                    if (!info.IsUwp)
                    {
                        bool isSystemApp = exePath.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase) ||
                                           exePath.Contains("System32", StringComparison.OrdinalIgnoreCase) ||
                                           lnk.Contains("Windows Administrative Tools", StringComparison.OrdinalIgnoreCase) ||
                                           lnk.Contains("Windows System", StringComparison.OrdinalIgnoreCase) ||
                                           lnk.Contains("Windows Accessories", StringComparison.OrdinalIgnoreCase) ||
                                           lnk.Contains("Windows PowerShell", StringComparison.OrdinalIgnoreCase) ||
                                           lnk.Contains("System Tools", StringComparison.OrdinalIgnoreCase) ||
                                           lnk.Contains("Accessibility", StringComparison.OrdinalIgnoreCase);

                        if (isSystemApp && !AllowedWindowsApps.Contains(name))
                            continue;
                    }

                    // Deduplicate by Name instead of exe path to prevent duplicate entries
                    var key = name.ToLowerInvariant();
                    if (!seen.Add(key)) continue;

                    // For UWP apps the .lnk is the canonical source for icons
                    // (resolves through the AppUserModelId). For Win32 apps the .exe
                    // target gives the real icon without shortcut overlay.
                    var iconPath = info.IsUwp ? lnk : exePath;

                    results.Add(new SearchResult
                    {
                        Id       = $"app:{exePath}",
                        Type     = ResultType.App,
                        Name     = name,
                        Subtitle = exePath,
                        IconPath = iconPath,
                        ExePath  = exePath,
                        LnkPath  = lnk,
                    });
                }
                catch { }
            }
        }

        // ── Desktop shortcuts (top-level only) ─────────────────
        if (Directory.Exists(DesktopPath))
        {
            foreach (var lnk in Directory.EnumerateFiles(DesktopPath, "*.lnk", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var info = ResolveLnk(lnk);
                    var exePath = info.TargetPath;
                    if (string.IsNullOrEmpty(exePath)) continue;

                    var name = Path.GetFileNameWithoutExtension(lnk);

                    if (JunkNames.Contains(name)) continue;

                    var exeFile = Path.GetFileName(exePath);
                    if (UninstallerExes.Contains(exeFile)) continue;
                    if (IsUninstallerByName(name)) continue;
                    if (!info.IsUwp && NonAppTargets.Contains(exeFile)) continue;

                    var ext = Path.GetExtension(exePath);
                    if (!string.IsNullOrEmpty(ext))
                    {
                        var extLower = ext.ToLowerInvariant();
                        if (extLower is not (".exe" or ".lnk" or ".msc" or ".bat" or ".cmd" or ".com"))
                            continue;
                    }

                    // Deduplicate
                    var key = name.ToLowerInvariant();
                    if (!seen.Add(key)) continue;

                    var iconPath = info.IsUwp ? lnk : exePath;

                    results.Add(new SearchResult
                    {
                        Id       = $"app:{exePath}",
                        Type     = ResultType.App,
                        Name     = name,
                        Subtitle = exePath,
                        IconPath = iconPath,
                        ExePath  = exePath,
                        LnkPath  = lnk,
                    });
                }
                catch { }
            }
        }

        // ── %LocalAppData%\Programs scan (apps without Start Menu shortcuts) ──
        DiscoverLocalPrograms(results, seen);
        DiscoverKnownApps(results, seen);
        DiscoverAppPaths(results, seen);
        DiscoverUninstallEntries(results, seen);

        // Sort alphabetically by name
        results.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    // ── LocalAppData\Programs scan ──────────────────────────────

    private static readonly string _localAppsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs");

    private void DiscoverLocalPrograms(List<SearchResult> results, HashSet<string> seen)
    {
        if (!Directory.Exists(_localAppsPath)) return;

        foreach (var appDir in Directory.EnumerateDirectories(_localAppsPath))
        {
            try
            {
                var dirName = Path.GetFileName(appDir);

                // Find best .exe: prefer one matching the directory name
                var exes = Directory.EnumerateFiles(appDir, "*.exe", SearchOption.TopDirectoryOnly)
                    .Where(e => !UninstallerExes.Contains(Path.GetFileName(e)))
                    .ToList();
                if (exes.Count == 0)
                {
                    exes = Directory.EnumerateDirectories(appDir)
                        .Take(8)
                        .SelectMany(d =>
                        {
                            try { return Directory.EnumerateFiles(d, "*.exe", SearchOption.TopDirectoryOnly); }
                            catch { return []; }
                        })
                        .Where(e => !UninstallerExes.Contains(Path.GetFileName(e)))
                        .ToList();
                }

                if (exes.Count == 0) continue;

                var bestExe = exes.FirstOrDefault(e =>
                    Path.GetFileNameWithoutExtension(e).Equals(dirName, StringComparison.OrdinalIgnoreCase))
                    ?? exes[0];

                var appName = Path.GetFileNameWithoutExtension(bestExe);

                if (JunkNames.Contains(appName)) continue;
                if (IsUninstallerByName(appName)) continue;
                if (NonAppTargets.Contains(Path.GetFileName(bestExe))) continue;

                var key = appName.ToLowerInvariant();
                if (!seen.Add(key)) continue;

                results.Add(new SearchResult
                {
                    Id       = $"app:{bestExe}",
                    Type     = ResultType.App,
                    Name     = appName,
                    Subtitle = bestExe,
                    IconPath = bestExe,
                    ExePath  = bestExe,
                });
            }
            catch { }
        }
    }

    private void DiscoverKnownApps(List<SearchResult> results, HashSet<string> seen)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        (string Name, string Path)[] candidates =
        [
            ("Claude", Path.Combine(local, "Programs", "Claude", "Claude.exe")),
            ("Claude", Path.Combine(local, "AnthropicClaude", "Claude.exe")),
            ("Firefox", Path.Combine(pf, "Mozilla Firefox", "firefox.exe")),
            ("Firefox", Path.Combine(pf86, "Mozilla Firefox", "firefox.exe")),
            ("Notepad", Path.Combine(windows, "notepad.exe")),
        ];

        foreach (var (name, path) in candidates)
            AddExeResult(results, seen, name, path);
    }

    private void DiscoverAppPaths(List<SearchResult> results, HashSet<string> seen)
    {
        const string subkey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
        foreach (var hive in new[] { Microsoft.Win32.Registry.CurrentUser, Microsoft.Win32.Registry.LocalMachine })
        {
            try
            {
                using var root = hive.OpenSubKey(subkey);
                if (root is null) continue;
                foreach (var name in root.GetSubKeyNames())
                {
                    using var key = root.OpenSubKey(name);
                    var path = key?.GetValue(null) as string;
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    AddExeResult(results, seen, Path.GetFileNameWithoutExtension(name), Environment.ExpandEnvironmentVariables(path.Trim('"')));
                }
            }
            catch { }
        }
    }

    private void DiscoverUninstallEntries(List<SearchResult> results, HashSet<string> seen)
    {
        string[] roots =
        [
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        ];

        foreach (var hive in new[] { Microsoft.Win32.Registry.CurrentUser, Microsoft.Win32.Registry.LocalMachine })
        foreach (var rootName in roots)
        {
            try
            {
                using var root = hive.OpenSubKey(rootName);
                if (root is null) continue;
                foreach (var sub in root.GetSubKeyNames())
                {
                    using var key = root.OpenSubKey(sub);
                    var display = key?.GetValue("DisplayName") as string;
                    var install = key?.GetValue("InstallLocation") as string;
                    if (string.IsNullOrWhiteSpace(display) || string.IsNullOrWhiteSpace(install) || !Directory.Exists(install)) continue;
                    var exe = Directory.EnumerateFiles(install, "*.exe", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(e => !UninstallerExes.Contains(Path.GetFileName(e)) && !NonAppTargets.Contains(Path.GetFileName(e)));
                    if (!string.IsNullOrWhiteSpace(exe))
                        AddExeResult(results, seen, display, exe);
                }
            }
            catch { }
        }
    }

    private static void AddExeResult(List<SearchResult> results, HashSet<string> seen, string name, string path)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        var cleanName = name.Trim();
        if (cleanName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            cleanName = Path.GetFileNameWithoutExtension(cleanName);
        if (!seen.Add(cleanName.ToLowerInvariant())) return;

        results.Add(new SearchResult
        {
            Id       = $"app:{path}",
            Type     = ResultType.App,
            Name     = cleanName,
            Subtitle = path,
            IconPath = path,
            ExePath  = path,
        });
    }

    // ── Filters ───────────────────────────────────────────────────

    private bool IsUninstallerByName(string name)
    {
        // English + common patterns
        if (name.StartsWith("Uninstall", StringComparison.OrdinalIgnoreCase))
            return true;

        // Multi-language uninstaller prefixes
        string[] prefixes =
        [
            "卸载", "卸載", "видалити", "удалить", "désinstaller",
            "アンインストール", "deïnstalleren", "odinstaluj",
            "afinstallere", "deinstallieren", "삭제", "деинсталирај",
            "desinstalar", "disinstallare", "avinstallere",
            "odinštalovať", "kaldır", "odinstalovat",
            "إلغاء التثبيت", "gỡ bỏ", "הסרה",
        ];

        foreach (var p in prefixes)
            if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private bool IsStartupEntry(string lnkPath)
    {
        foreach (var dir in StartUpPaths)
        {
            if (!Directory.Exists(dir)) continue;
            if (lnkPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Resolves a .lnk shortcut to its target path and detects whether
    /// it is a UWP / Microsoft Store app (launched via explorer.exe
    /// shell:AppsFolder or living under WindowsApps).
    /// </summary>
    private LnkInfo ResolveLnk(string lnkPath)
    {
        try
        {
            var link    = (IShellLinkW)new ShellLink();
            var persist = (IPersistFile)link;
            persist.Load(lnkPath, 0);

            var sb = new StringBuilder(260);
            link.GetPath(sb, sb.Capacity, IntPtr.Zero, 0);
            var target = sb.ToString();

            if (string.IsNullOrWhiteSpace(target))
                target = lnkPath;

            // Detect UWP / Store apps:
            //   - Target is explorer.exe with shell:AppsFolder arguments
            //   - Target lives inside C:\Program Files\WindowsApps
            bool isUwp = false;
            var targetFile = Path.GetFileName(target);
            if (targetFile.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
            {
                var argsSb = new StringBuilder(260);
                link.GetArguments(argsSb, argsSb.Capacity);
                if (argsSb.ToString().Contains("shell:AppsFolder", StringComparison.OrdinalIgnoreCase))
                    isUwp = true;
            }
            else if (target.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase))
            {
                isUwp = true;
            }

            return new LnkInfo(target, isUwp);
        }
        catch
        {
            return new LnkInfo(lnkPath, false);
        }
    }

    // ── Cache ─────────────────────────────────────────────────────

    private List<SearchResult>? LoadCache()
    {
        try
        {
            if (!File.Exists(CachePath)) return null;
            // Invalidate stale cache so newly installed apps are picked up (24 hours)
            if ((DateTime.Now - File.GetLastWriteTime(CachePath)).TotalHours > 24)
                return null;
            var dtos = JsonSerializer.Deserialize<List<PersistedSearchResult>>(
                File.ReadAllText(CachePath));
            return dtos?.ConvertAll(d => d.ToResult());
        }
        catch { return null; }
    }

    private void SaveCache(List<SearchResult> catalog)
    {
        try
        {
            var dtos = catalog.ConvertAll(PersistedSearchResult.FromResult);
            File.WriteAllText(CachePath,
                JsonSerializer.Serialize(dtos, JsonOpts));
        }
        catch (Exception ex)
        {
            _logger.Warning("Cache save failed", ex);
        }
    }

    /// <summary>Deletes the on-disk catalog so the next <see cref="DiscoverAsync"/> call runs a full scan.</summary>
    public void ClearCache()
    {
        try { if (File.Exists(CachePath)) File.Delete(CachePath); }
        catch { }
    }
}
