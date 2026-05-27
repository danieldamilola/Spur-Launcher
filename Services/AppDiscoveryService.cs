using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Volt.Services;

/// <summary>
/// Discovers installed applications from Windows Start Menu shortcuts.
/// Resolves .lnk files using COM IShellLink to get the real executable path.
/// </summary>
public sealed class AppDiscoveryService
{
    private static readonly string[] StartMenuPaths =
    [
        Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
        Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
    ];

    // COM GUIDs for IShellLink
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

    /// <summary>Loads all Start Menu .lnk files and resolves their target executables.</summary>
    public Task<List<SearchResult>> DiscoverAsync()
    {
        // COM interop needs STA — Task.Run uses MTA threads
        var tcs = new TaskCompletionSource<List<SearchResult>>();
        var thread = new Thread(() =>
        {
            try { tcs.SetResult(Discover()); }
            catch (Exception ex) { tcs.SetException(ex); }
        })
        {
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }

    private List<SearchResult> Discover()
    {
        var results = new List<SearchResult>();
        // Track UWP AUMIDs to avoid adding the same UWP app twice
        var seenUwp = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Track names so we can replace UWP entries with richer .lnk entries when both exist
        var byName  = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // 1. Discover UWP and modern apps via shell:AppsFolder
        try
        {
            Type? shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is not null)
            {
                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic folder = shell.NameSpace("shell:::{4234d49b-0245-4df3-b780-3893943456e1}");
                if (folder is not null)
                {
                    foreach (dynamic item in folder.Items())
                    {
                        string name = item.Name;
                        string path = item.Path; // This is the AppUserModelId
                        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path)) continue;
                        if (!seenUwp.Add(path)) continue;

                        var result = new SearchResult
                        {
                            Id       = $"app:{path}",
                            Type     = ResultType.App,
                            Name     = name,
                            Subtitle = "App",
                            IconPath = null,
                            ExePath  = @"shell:AppsFolder\" + path,
                        };

                        results.Add(result);
                        byName[name] = results.Count - 1;
                    }
                }
            }
        }
        catch (InvalidCastException) { /* COM interface not registered — skip UWP */ }
        catch (COMException)          { /* COM error — skip UWP */           }
        catch { /* ignore other errors in COM shell extraction */ }

        // 2. Discover classic Start Menu shortcuts
        var seenLnk = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in StartMenuPaths)
        {
            if (!Directory.Exists(root)) continue;

            foreach (var lnk in Directory.EnumerateFiles(root, "*.lnk", SearchOption.AllDirectories))
            {
                try
                {
                    var exePath = ResolveLnk(lnk);
                    if (string.IsNullOrEmpty(exePath)) continue;

                    var name = Path.GetFileNameWithoutExtension(lnk);
                    if (name.Contains("Uninstall", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.StartsWith("—", StringComparison.Ordinal)) continue;

                    // Deduplicate .lnk shortcuts by exe path
                    if (!seenLnk.Add(exePath)) continue;

                    var result = new SearchResult
                    {
                        Id       = $"app:{exePath}",
                        Type     = ResultType.App,
                        Name     = name,
                        Subtitle = exePath,
                        IconPath = exePath,
                        ExePath  = exePath,
                        LnkPath  = lnk,
                    };

                    // If a UWP entry with the same name exists, replace it (lnk has richer metadata + icon)
                    if (byName.TryGetValue(name, out var idx))
                    {
                        results[idx] = result;
                        byName[name] = idx;
                    }
                    else
                    {
                        results.Add(result);
                        byName[name] = results.Count - 1;
                    }
                }
                catch { /* skip invalid shortcuts */ }
            }
        }

        return results;
    }

    private static string ResolveLnk(string lnkPath)
    {
        try
        {
            var link    = (IShellLinkW)new ShellLink();
            var persist = (IPersistFile)link;
            persist.Load(lnkPath, 0);

            var sb = new StringBuilder(260);
            link.GetPath(sb, sb.Capacity, IntPtr.Zero, 0);
            var path = sb.ToString();

            return string.IsNullOrWhiteSpace(path) ? lnkPath : path;
        }
        catch
        {
            return lnkPath;
        }
    }
}
