using System.Runtime.InteropServices;
using System.Text;

namespace Spur.Services.FileSearch;

internal sealed class EverythingProvider
{
    private const string DllName = "Everything.dll";
    private const int BufferSize = 4096;
    private static readonly StringBuilder Buffer = new(BufferSize);
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static int _loadAttempted;
    private static bool _loadSucceeded;
    private static bool _everythingRunning;
    private static bool _runningChecked;

    public static bool IsAvailable
    {
        get
        {
            if (_runningChecked) return _loadSucceeded && _everythingRunning;
            if (_loadAttempted == 0)
            {
                Interlocked.Exchange(ref _loadAttempted, 1);
                try
                {
                    var path = Path.Combine(AppContext.BaseDirectory, DllName);
                    if (File.Exists(path))
                        _loadSucceeded = NativeMethods.LoadLibraryW(path) != 0;
                }
                catch { }
            }
            if (!_loadSucceeded) return false;
            var ver = NativeMethods.Everything_GetMajorVersion();
            var err = NativeMethods.Everything_GetLastError();
            _everythingRunning = err != 1 && ver > 0;
            _runningChecked = true;
            return _everythingRunning;
        }
    }

    public static async Task<List<string>> SearchAsync(string query, int maxCount, CancellationToken ct)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(query))
            return [];

        await Semaphore.WaitAsync(ct);
        try
        {
            if (ct.IsCancellationRequested) return [];

            NativeMethods.Everything_SetSearchW(query);
            NativeMethods.Everything_SetMax(maxCount);
            NativeMethods.Everything_SetMatchPath(false);
            NativeMethods.Everything_SetMatchCase(false);
            NativeMethods.Everything_SetRegex(false);

            if (ct.IsCancellationRequested) return [];

            if (!NativeMethods.Everything_QueryW(true))
                return [];

            var count = NativeMethods.Everything_GetNumResults();
            var results = new List<string>(count);

            for (int i = 0; i < count; i++)
            {
                if (ct.IsCancellationRequested) break;
                NativeMethods.Everything_GetResultFullPathNameW(i, Buffer, BufferSize);
                results.Add(Buffer.ToString());
                Buffer.Clear();
            }

            return results;
        }
        finally
        {
            NativeMethods.Everything_Reset();
            Semaphore.Release();
        }
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern nint LoadLibraryW(string lpFileName);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        public static extern int Everything_SetSearchW(string lpSearchString);

        [DllImport(DllName)]
        public static extern void Everything_SetMatchPath(bool bEnable);

        [DllImport(DllName)]
        public static extern void Everything_SetMatchCase(bool bEnable);

        [DllImport(DllName)]
        public static extern void Everything_SetRegex(bool bEnable);

        [DllImport(DllName)]
        public static extern void Everything_SetMax(int dwMax);

        [DllImport(DllName)]
        public static extern bool Everything_QueryW(bool bWait);

        [DllImport(DllName)]
        public static extern int Everything_GetNumResults();

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        public static extern void Everything_GetResultFullPathNameW(int nIndex, StringBuilder lpString, int nMaxCount);

        [DllImport(DllName)]
        public static extern bool Everything_IsFolderResult(int nIndex);

        [DllImport(DllName)]
        public static extern bool Everything_IsFileResult(int nIndex);

        [DllImport(DllName)]
        public static extern void Everything_Reset();

        [DllImport(DllName)]
        public static extern int Everything_GetMajorVersion();

        [DllImport(DllName)]
        public static extern int Everything_GetLastError();
    }
}
