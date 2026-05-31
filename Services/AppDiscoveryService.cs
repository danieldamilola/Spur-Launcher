using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Arc.Models;

namespace Arc.Services;

/// <summary>
/// Discovers user-facing installed applications from multiple sources:
/// - Start Menu shortcuts (primary source)
/// - Desktop shortcuts
/// - UWP/Store apps (via Windows API)
/// - Registry (App Paths + Uninstall entries)
/// - PATH environment variable
/// - %LocalAppData%\Programs
///
/// Inspired by Flow Launcher's Program plugin with configurable index sources.
/// </summary>
public interface IAppDiscoveryService
{
    Task<List<SearchResult>> DiscoverAsync(CancellationToken ct = default);
    void ClearCache();
}

public sealed class AppDiscoveryService : IAppDiscoveryService
{
    private readonly ILogger _logger;
    private readonly ArcConfig _config;

    public AppDiscoveryService(ILogger logger, ArcConfig config)
    {
        _logger = logger;
        _config = config;
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
        "unwise.exe", "unwise32.exe", "unins001.exe", "unins002.exe",
        "setup.exe", "installer.exe", "install.exe",
        "maintenancetool.exe", "modify.exe", "repair.exe",
    };

    // ── Shortcut names that are never real apps ───────────────────
    private static readonly HashSet<string> JunkNames =
        new(StringComparer.OrdinalIgnoreCase)
    {
        "uninstall", "uninstaller", "remove", "setup", "install", "installer",
        "help", "readme", "license", "licence",
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
        "modify", "maintenance", "maintenancetool",
    };

    // ── .lnk target executables that are never user apps ──────────
    private static readonly HashSet<string> NonAppTargets =
        new(StringComparer.OrdinalIgnoreCase)
    {
        "rundll32.exe", "wscript.exe", "cscript.exe", "mshta.exe",
        "dxdiag.exe", "winver.exe", "charmap.exe",
        "cleanmgr.exe", "diskpart.exe", "diskmgmt.msc",
        "services.msc", "devmgmt.msc", "eventvwr.msc",
        "perfmon.msc", "taskschd.msc", "gpedit.msc",
        "secpol.msc", "compmgmt.msc", "wf.msc",
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
    // Discovery (multi-source like Flow Launcher's Program plugin)
    // ══════════════════════════════════════════════════════════════

    private List<SearchResult> DiscoverAll()
    {
        var results = new List<SearchResult>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // ── Source 1: Start Menu (if enabled) ────────────────────────
        if (_config.IndexStartMenu)
        {
            DiscoverStartMenu(results, seen);
        }

        // ── Source 2: Desktop shortcuts ─────────────────────────────
        DiscoverDesktop(results, seen);

        // ── Source 3: UWP Apps via Windows API (if enabled) ────────
        if (_config.IndexUwpApps)
        {
            DiscoverUwpApps(results, seen);
        }

        // ── Source 4: LocalAppData\Programs ─────────────────────────
        DiscoverLocalPrograms(results, seen);

        // ── Source 5: Known hardcoded app locations ────────────────
        DiscoverKnownApps(results, seen);

        // ── Source 6: Registry App Paths (if enabled) ───────────────
        if (_config.IndexRegistry)
        {
            DiscoverAppPaths(results, seen);
            DiscoverUninstallEntries(results, seen);
        }

        // ── Source 7: PATH environment variable (if enabled) ───────
        if (_config.IndexPath)
        {
            DiscoverPathApps(results, seen);
        }

        // Sort alphabetically by name
        results.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    // ── Source 1: Start Menu ───────────────────────────────────────
    private void DiscoverStartMenu(List<SearchResult> results, HashSet<string> seen)
    {
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

                    if (JunkNames.Contains(name))
                        continue;

                    var exeFile = Path.GetFileName(exePath);
                    if (UninstallerExes.Contains(exeFile))
                        continue;

                    if (IsUninstallerByName(name))
                        continue;

                    // Skip startup entries
                    if (IsStartupEntry(lnk)) continue;

                    // Skip shortcuts that resolve to non-user-app launchers.
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
                            continue;
                    }

                    // Filter Windows/System clutter - less aggressive than before
                    // Only block true system binaries, allow most user-facing apps
                    if (!info.IsUwp && IsSystemBinary(exePath))
                        continue;

                    // Deduplicate by Name
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
    }

    // ── Source 2: Desktop shortcuts ───────────────────────────────
    private void DiscoverDesktop(List<SearchResult> results, HashSet<string> seen)
    {
        if (!Directory.Exists(DesktopPath)) return;

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

    // ── Source 3: UWP Apps via shell:AppsFolder enumeration ─────────
    private void DiscoverUwpApps(List<SearchResult> results, HashSet<string> seen)
    {
        // Enumerate UWP apps via shell:AppsFolder using COM shell APIs
        // This works without the Windows.Management.Deployment SDK
        try
        {
            var shellApps = GetUwpAppsFromShell();
            foreach (var (name, appUserModelId) in shellApps)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (JunkNames.Contains(name)) continue;

                    var key = name.ToLowerInvariant();
                    if (!seen.Add(key)) continue;

                    // UWP apps are launched via shell:AppsFolder
                    var exePath = $"shell:AppsFolder\\{appUserModelId}";

                    results.Add(new SearchResult
                    {
                        Id       = $"uwp:{appUserModelId}",
                        Type     = ResultType.App,
                        Name     = name,
                        Subtitle = "Windows App",
                        IconPath = exePath,
                        ExePath  = exePath,
                        LnkPath  = null,
                    });
                }
                catch { continue; }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning("UWP app discovery failed", ex);
        }
    }

    /// <summary>
    /// Enumerates UWP apps from multiple sources:
    /// 1. Start Menu UWP shortcuts (most reliable)
    /// 2. Get-StartApps PowerShell (for user-pinned apps)
    /// 3. WindowsApps directory scan (fallback)
    /// </summary>
    private List<(string Name, string AppUserModelId)> GetUwpAppsFromShell()
    {
        var apps = new List<(string Name, string AppUserModelId)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Method 1: Scan Start Menu for UWP app shortcuts (most reliable)
        foreach (var root in StartMenuPaths)
        {
            try
            {
                if (!Directory.Exists(root)) continue;

                // UWP apps in Start Menu have shortcuts that point to explorer.exe shell:AppsFolder
                foreach (var lnk in Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories))
                {
                    try
                    {
                        var info = ResolveLnk(lnk);
                        var target = info.TargetPath;

                        // Check if this is a UWP app shortcut
                        if (!target.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Get the .lnk name as the app name
                        var name = Path.GetFileNameWithoutExtension(lnk);
                        if (JunkNames.Contains(name)) continue;

                        // Extract AppUserModelId from the shortcut arguments
                        var appUserModelId = GetUwpAppIdFromLnk(lnk);
                        if (string.IsNullOrWhiteSpace(appUserModelId)) continue;

                        var key = name.ToLowerInvariant();
                        if (!seen.Add(key)) continue;

                        apps.Add((name, appUserModelId));
                    }
                    catch { continue; }
                }
            }
            catch { }
        }

        // Method 2: Use Get-StartApps PowerShell to get additional apps
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"Get-StartApps | Select-Object Name, AppID | ConvertTo-Json -Compress\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc != null)
            {
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(5000);

                if (!string.IsNullOrWhiteSpace(output))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(output);
                        if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var element in doc.RootElement.EnumerateArray())
                            {
                                var name = element.GetProperty("Name").GetString();
                                var appId = element.GetProperty("AppID").GetString();
                                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(appId))
                                {
                                    var key = name.ToLowerInvariant();
                                    if (seen.Add(key))
                                    {
                                        apps.Add((name, appId));
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        return apps;
    }

    /// <summary>
    /// Extracts the AppUserModelId from a UWP app shortcut (.lnk file).
    /// UWP shortcuts target "explorer.exe shell:AppsFolder\{AppUserModelId}"
    /// </summary>
    private string? GetUwpAppIdFromLnk(string lnkPath)
    {
        try
        {
            var link = (IShellLinkW)new ShellLink();
            var persist = (IPersistFile)link;
            persist.Load(lnkPath, 0);

            // Get the target path
            var sbPath = new StringBuilder(260);
            link.GetPath(sbPath, sbPath.Capacity, IntPtr.Zero, 0);
            var target = sbPath.ToString();

            // Only process shortcuts to explorer.exe
            if (!Path.GetFileName(target).Equals("explorer.exe", StringComparison.OrdinalIgnoreCase))
                return null;

            // Get the arguments (contains the AppUserModelId)
            var sbArgs = new StringBuilder(260);
            link.GetArguments(sbArgs, sbArgs.Capacity);
            var args = sbArgs.ToString();

            // Extract AppUserModelId from "shell:AppsFolder\AppUserModelId"
            if (args.Contains("shell:AppsFolder"))
            {
                var parts = args.Split('\\');
                if (parts.Length > 1)
                {
                    return parts[^1].Trim(); // Last part is the AppUserModelId
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // ── Source 7: PATH environment variable ──────────────────────
    private void DiscoverPathApps(List<SearchResult> results, HashSet<string> seen)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var paths = pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pathDir in paths)
        {
            try
            {
                if (!Directory.Exists(pathDir)) continue;

                // Skip system directories that are already covered
                var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                if (pathDir.StartsWith(windowsDir, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var exe in Directory.EnumerateFiles(pathDir, "*.exe", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        var name = Path.GetFileNameWithoutExtension(exe);

                        // Skip junk names and uninstallers
                        if (JunkNames.Contains(name)) continue;
                        if (UninstallerExes.Contains(Path.GetFileName(exe))) continue;
                        if (IsUninstallerByName(name)) continue;
                        if (NonAppTargets.Contains(Path.GetFileName(exe))) continue;

                        // Skip if already found via other source
                        var key = name.ToLowerInvariant();
                        if (!seen.Add(key)) continue;

                        results.Add(new SearchResult
                        {
                            Id       = $"path:{exe}",
                            Type     = ResultType.App,
                            Name     = name,
                            Subtitle = exe,
                            IconPath = exe,
                            ExePath  = exe,
                            LnkPath  = null,
                        });
                    }
                    catch { continue; }
                }
            }
            catch { continue; }
        }
    }

    // ── System binary blocklist (static — never re-allocated) ──────
    private static readonly HashSet<string> _blockedSystemExes =
        new(StringComparer.OrdinalIgnoreCase)
    {
        "svchost.exe", "csrss.exe", "smss.exe", "services.exe", "lsass.exe",
        "wininit.exe", "winlogon.exe", "crss.exe", "dllhost.exe", "taskhostw.exe",
        "sihost.exe", "fontdrvhost.exe", "dwm.exe", "conhost.exe",
    };

    // Helper: Check if an exe is a system binary
    private static bool IsSystemBinary(string exePath)
        => _blockedSystemExes.Contains(Path.GetFileName(exePath));

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
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Comprehensive list of known app locations including package managers
        (string Name, string Path)[] candidates =
        [
            // Claude Desktop (Anthropic)
            ("Claude", Path.Combine(local, "Programs", "Claude", "Claude.exe")),
            ("Claude", Path.Combine(local, "AnthropicClaude", "Claude.exe")),
            ("Claude", Path.Combine(pf, "Anthropic", "Claude", "Claude.exe")),
            ("Claude", Path.Combine(pf86, "Anthropic", "Claude", "Claude.exe")),

            // Codex (OpenAI CLI tool - if installed as standalone)
            ("Codex", Path.Combine(userProfile, ".codex", "codex.exe")),
            ("Codex", Path.Combine(userProfile, ".local", "bin", "codex.exe")),

            // Firefox
            ("Firefox", Path.Combine(pf, "Mozilla Firefox", "firefox.exe")),
            ("Firefox", Path.Combine(pf86, "Mozilla Firefox", "firefox.exe")),

            // Notepad (Windows built-in)
            ("Notepad", Path.Combine(windows, "notepad.exe")),
            ("Notepad", Path.Combine(windows, "System32", "notepad.exe")),

            // VS Code
            ("Visual Studio Code", Path.Combine(pf, "Microsoft VS Code", "Code.exe")),
            ("Visual Studio Code", Path.Combine(pf86, "Microsoft VS Code", "Code.exe")),
            ("VS Code", Path.Combine(local, "Programs", "Microsoft VS Code", "Code.exe")),

            // Cursor (AI editor)
            ("Cursor", Path.Combine(local, "Programs", "cursor", "Cursor.exe")),
            ("Cursor", Path.Combine(pf, "Cursor", "Cursor.exe")),

            // Windsurf
            ("Windsurf", Path.Combine(local, "Programs", "windsurf", "Windsurf.exe")),

            // Zed
            ("Zed", Path.Combine(pf, "Zed", "zed.exe")),
            ("Zed", Path.Combine(userProfile, "scoop", "apps", "zed", "current", "zed.exe")),

            // Sublime Text
            ("Sublime Text", Path.Combine(pf, "Sublime Text", "sublime_text.exe")),
            ("Sublime Text", Path.Combine(pf86, "Sublime Text", "sublime_text.exe")),

            // JetBrains Toolbox apps
            ("WebStorm", Path.Combine(pf, "JetBrains", "WebStorm", "bin", "webstorm64.exe")),
            ("IntelliJ IDEA", Path.Combine(pf, "JetBrains", "IntelliJ IDEA", "bin", "idea64.exe")),
            ("PyCharm", Path.Combine(pf, "JetBrains", "PyCharm", "bin", "pycharm64.exe")),
            ("Rider", Path.Combine(pf, "JetBrains", "Rider", "bin", "rider64.exe")),
            ("GoLand", Path.Combine(pf, "JetBrains", "GoLand", "bin", "goland64.exe")),
            ("PhpStorm", Path.Combine(pf, "JetBrains", "PhpStorm", "bin", "phpstorm64.exe")),

            // Node.js / NPM global packages
            ("Node.js", Path.Combine(pf, "nodejs", "node.exe")),
            ("Node.js", Path.Combine(pf86, "nodejs", "node.exe")),

            // Docker Desktop
            ("Docker Desktop", Path.Combine(pf, "Docker", "Docker", "Docker Desktop.exe")),

            // Figma
            ("Figma", Path.Combine(pf, "Figma", "Figma.exe")),
            ("Figma", Path.Combine(pf86, "Figma", "Figma.exe")),
            ("Figma", Path.Combine(userProfile, "AppData", "Local", "Figma", "Figma.exe")),

            // Discord
            ("Discord", Path.Combine(pf, "Discord", "Discord.exe")),
            ("Discord", Path.Combine(pf86, "Discord", "Discord.exe")),
            ("Discord", Path.Combine(local, "Discord", "Discord.exe")),
            ("Discord", Path.Combine(userProfile, "scoop", "apps", "discord", "current", "Discord.exe")),

            // Slack
            ("Slack", Path.Combine(pf, "Slack", "slack.exe")),
            ("Slack", Path.Combine(local, "slack", "slack.exe")),
            ("Slack", Path.Combine(userProfile, "scoop", "apps", "slack", "current", "slack.exe")),

            // Telegram Desktop
            ("Telegram Desktop", Path.Combine(pf, "Telegram Desktop", "Telegram.exe")),
            ("Telegram Desktop", Path.Combine(pf86, "Telegram Desktop", "Telegram.exe")),
            ("Telegram Desktop", Path.Combine(userProfile, "scoop", "apps", "telegram", "current", "Telegram.exe")),

            // WhatsApp
            ("WhatsApp", Path.Combine(pf, "WindowsApps", "WhatsApp.exe")),
            ("WhatsApp", Path.Combine(local, "WhatsApp", "WhatsApp.exe")),

            // Spotify
            ("Spotify", Path.Combine(pf, "Spotify", "Spotify.exe")),
            ("Spotify", Path.Combine(pf86, "Spotify", "Spotify.exe")),
            ("Spotify", Path.Combine(userProfile, "scoop", "apps", "spotify", "current", "Spotify.exe")),

            // Obsidian
            ("Obsidian", Path.Combine(pf, "Obsidian", "Obsidian.exe")),
            ("Obsidian", Path.Combine(userProfile, "scoop", "apps", "obsidian", "current", "Obsidian.exe")),

            // Notion
            ("Notion", Path.Combine(pf, "Notion", "Notion.exe")),
            ("Notion", Path.Combine(userProfile, "scoop", "apps", "notion", "current", "Notion.exe")),

            // Postman
            ("Postman", Path.Combine(pf, "Postman", "Postman.exe")),
            ("Postman", Path.Combine(local, "Postman", "Postman.exe")),

            // Insomnia
            ("Insomnia", Path.Combine(pf, "Insomnia", "Insomnia.exe")),
            ("Insomnia", Path.Combine(pf86, "Insomnia", "Insomnia.exe")),

            // DBeaver
            ("DBeaver", Path.Combine(pf, "DBeaver", "dbeaver.exe")),

            // GitHub Desktop
            ("GitHub Desktop", Path.Combine(local, "GitHubDesktop", "GitHubDesktop.exe")),

            // SourceTree
            ("SourceTree", Path.Combine(pf, "Atlassian", "SourceTree", "SourceTree.exe")),

            // Fork (git client)
            ("Fork", Path.Combine(pf, "Fork", "Fork.exe")),
            ("Fork", Path.Combine(local, "Fork", "Fork.exe")),

            // Windows Terminal
            ("Windows Terminal", Path.Combine(pf, "WindowsApps", "Microsoft.WindowsTerminal_8wekyb3d8bbwe", "wt.exe")),
            ("Windows Terminal", Path.Combine(local, "Microsoft", "WindowsApps", "wt.exe")),

            // PowerShell 7
            ("PowerShell 7", Path.Combine(pf, "PowerShell", "7", "pwsh.exe")),

            // WinGet
            ("WinGet", Path.Combine(pf, "WindowsApps", "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe", "winget.exe")),
            ("WinGet", Path.Combine(local, "Microsoft", "WindowsApps", "winget.exe")),
        ];

        foreach (var (name, path) in candidates)
            AddExeResult(results, seen, name, path);

        // Scan common package manager directories
        DiscoverPackageManagerApps(results, seen, userProfile, local);
    }

    // Scan package manager install directories (Scoop, Chocolatey, etc.)
    private void DiscoverPackageManagerApps(List<SearchResult> results, HashSet<string> seen, string userProfile, string localAppData)
    {
        // Scoop apps
        var scoopPath = Path.Combine(userProfile, "scoop", "apps");
        if (Directory.Exists(scoopPath))
        {
            foreach (var appDir in Directory.EnumerateDirectories(scoopPath).Take(100))
            {
                try
                {
                    var appName = Path.GetFileName(appDir);
                    if (appName.Equals("scoop", StringComparison.OrdinalIgnoreCase)) continue;

                    var currentDir = Path.Combine(appDir, "current");
                    if (!Directory.Exists(currentDir)) continue;

                    // Find main exe in current directory
                    var exeFiles = Directory.EnumerateFiles(currentDir, "*.exe", SearchOption.TopDirectoryOnly)
                        .Where(e => !UninstallerExes.Contains(Path.GetFileName(e)))
                        .ToList();

                    if (exeFiles.Count == 0)
                    {
                        // Try app directory itself
                        exeFiles = Directory.EnumerateFiles(appDir, "*.exe", SearchOption.TopDirectoryOnly)
                            .Where(e => !UninstallerExes.Contains(Path.GetFileName(e)))
                            .ToList();
                    }

                    if (exeFiles.Count == 0) continue;

                    // Prefer exe matching app name
                    var bestExe = exeFiles.FirstOrDefault(e =>
                        Path.GetFileNameWithoutExtension(e).Equals(appName, StringComparison.OrdinalIgnoreCase))
                        ?? exeFiles.First();

                    var exeName = Path.GetFileNameWithoutExtension(bestExe);
                    if (JunkNames.Contains(exeName)) continue;
                    if (IsUninstallerByName(exeName)) continue;

                    var key = exeName.ToLowerInvariant();
                    if (!seen.Add(key)) continue;

                    results.Add(new SearchResult
                    {
                        Id       = $"scoop:{bestExe}",
                        Type     = ResultType.App,
                        Name     = exeName,
                        Subtitle = bestExe,
                        IconPath = bestExe,
                        ExePath  = bestExe,
                    });
                }
                catch { continue; }
            }
        }

        // Chocolatey apps
        var chocoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Chocolatey", "bin");
        if (Directory.Exists(chocoPath))
        {
            foreach (var exe in Directory.EnumerateFiles(chocoPath, "*.exe").Take(100))
            {
                try
                {
                    var name = Path.GetFileNameWithoutExtension(exe);
                    if (JunkNames.Contains(name)) continue;
                    if (UninstallerExes.Contains(Path.GetFileName(exe))) continue;
                    if (IsUninstallerByName(name)) continue;

                    var key = name.ToLowerInvariant();
                    if (!seen.Add(key)) continue;

                    results.Add(new SearchResult
                    {
                        Id       = $"choco:{exe}",
                        Type     = ResultType.App,
                        Name     = name,
                        Subtitle = exe,
                        IconPath = exe,
                        ExePath  = exe,
                    });
                }
                catch { continue; }
            }
        }
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

    private void AddExeResult(List<SearchResult> results, HashSet<string> seen, string name, string path)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
        var cleanName = name.Trim();
        if (cleanName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            cleanName = Path.GetFileNameWithoutExtension(cleanName);

        // Filter uninstallers at the central add point
        if (JunkNames.Contains(cleanName))
        {
            _logger.Info($"Filtered (junk): {cleanName}");
            return;
        }
        if (IsUninstallerByName(cleanName))
        {
            _logger.Info($"Filtered (uninstaller): {cleanName}");
            return;
        }
        // Also catch names containing "uninstall" or "help" anywhere (not just prefix)
        if (cleanName.Contains("Uninstall", StringComparison.OrdinalIgnoreCase) ||
            cleanName.Contains("Help", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info($"Filtered (contains uninstall/help): {cleanName}");
            return;
        }

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
        var lower = name.ToLowerInvariant();

        // Check for uninstaller keywords anywhere in the name
        string[] uninstallerKeywords =
        [
            "uninstall", "uninstaller", "remove",
        ];

        foreach (var kw in uninstallerKeywords)
        {
            if (lower.Contains(kw))
            {
                _logger.Info($"IsUninstallerByName matched '{kw}' in '{name}'");
                return true;
            }
        }

        // Check for installer keywords (but not "installer" as part of app name)
        // Only flag if it looks like a setup/installer tool, not the app itself
        if (lower.StartsWith("setup") ||
            lower.StartsWith("install") && !lower.Contains("instant") && !lower.Contains("instance") ||
            lower.EndsWith("setup") ||
            lower.EndsWith("installer") && !lower.Contains("steam") && !lower.Contains("epic"))
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
