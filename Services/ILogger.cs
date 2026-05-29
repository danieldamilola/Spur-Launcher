namespace Arc.Services;

/// <summary>
/// Simple logging abstraction for Arc. Production apps should use Microsoft.Extensions.Logging.
/// </summary>
public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message, Exception? exception = null);
    void Error(string message, Exception? exception = null);
    void Fatal(string message, Exception? exception = null);
}

/// <summary>
/// Logger implementation that writes to file and debug output.
/// Uses proper locking for thread safety.
/// </summary>
public sealed class FileLogger : ILogger, IDisposable
{
    private readonly string _logPath;
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private bool _disposed;

    public FileLogger(string? logDirectory = null)
    {
        logDirectory ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Arc", "Logs");
        
        Directory.CreateDirectory(logDirectory);
        
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _logPath = Path.Combine(logDirectory, $"arc_{timestamp}.log");
        
        _writer = new StreamWriter(_logPath, append: true) { AutoFlush = true };
        
        Info($"Logger initialized. Log file: {_logPath}");
    }

    private void WriteLog(string level, string message, Exception? exception = null)
    {
        if (_disposed) return;
        
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var threadId = Environment.CurrentManagedThreadId;
        var logLine = $"[{timestamp}] [{threadId:D3}] [{level,-5}] {message}";
        
        if (exception != null)
        {
            logLine += $"\n  Exception: {exception.GetType().Name}: {exception.Message}\n  Stack: {exception.StackTrace}";
        }

        lock (_lock)
        {
            _writer?.WriteLine(logLine);
        }
        
        System.Diagnostics.Debug.WriteLine(logLine);
    }

    public void Debug(string message) => WriteLog("DEBUG", message);
    public void Info(string message) => WriteLog("INFO", message);
    public void Warning(string message, Exception? exception = null) => WriteLog("WARN", message, exception);
    public void Error(string message, Exception? exception = null) => WriteLog("ERROR", message, exception);
    public void Fatal(string message, Exception? exception = null) => WriteLog("FATAL", message, exception);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}

/// <summary>
/// No-op logger for tests or when logging is disabled.
/// </summary>
public sealed class NullLogger : ILogger
{
    public static readonly NullLogger Instance = new();
    
    private NullLogger() { }
    
    public void Debug(string message) { }
    public void Info(string message) { }
    public void Warning(string message, Exception? exception = null) { }
    public void Error(string message, Exception? exception = null) { }
    public void Fatal(string message, Exception? exception = null) { }
}
