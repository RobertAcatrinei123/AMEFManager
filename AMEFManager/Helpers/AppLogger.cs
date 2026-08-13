using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AMEFManager.Helpers;

public static class AppLogger
{
#if DEBUG
    private static readonly object _lock = new();
    private static string? _logFilePath;
#endif

    public static string LogFilePath
    {
        get
        {
#if DEBUG
            lock (_lock)
            {
                if (string.IsNullOrEmpty(_logFilePath))
                {
                    _logFilePath = GetDefaultLogFilePath();
                }
                return _logFilePath;
            }
#else
            return string.Empty;
#endif
        }
        set
        {
#if DEBUG
            lock (_lock)
            {
                _logFilePath = value;
            }
#endif
        }
    }

    static AppLogger()
    {
#if DEBUG
        _logFilePath = GetDefaultLogFilePath();
#endif
    }

    [Conditional("DEBUG")]
    public static void Log(string message)
    {
#if DEBUG
        Write("INFO", message);
#endif
    }

    [Conditional("DEBUG")]
    public static void LogInfo(string message)
    {
#if DEBUG
        Write("INFO", message);
#endif
    }

    [Conditional("DEBUG")]
    public static void LogDebug(string message)
    {
#if DEBUG
        Write("DEBUG", message);
#endif
    }

    [Conditional("DEBUG")]
    public static void LogWarning(string message)
    {
#if DEBUG
        Write("WARN", message);
#endif
    }

    [Conditional("DEBUG")]
    public static void LogError(string message, Exception? ex = null)
    {
#if DEBUG
        if (ex != null)
        {
            string details = FormatException(ex);
            Write("ERROR", $"{message} - {details}");
        }
        else
        {
            Write("ERROR", message);
        }
#endif
    }

    [Conditional("DEBUG")]
    public static void LogException(Exception ex, string? context = null)
    {
#if DEBUG
        string details = FormatException(ex);
        string prefix = string.IsNullOrWhiteSpace(context) ? "Exception caught" : context;
        Write("ERROR", $"{prefix}: {details}");
#endif
    }

    [Conditional("DEBUG")]
    public static void ClearLog()
    {
#if DEBUG
        lock (_lock)
        {
            try
            {
                string path = LogFilePath;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to clear log file: {ex.Message}");
            }
        }
#endif
    }

#if DEBUG
    private static void Write(string level, string message)
    {
        lock (_lock)
        {
            try
            {
                string path = LogFilePath;
                if (string.IsNullOrEmpty(path))
                {
                    path = GetDefaultLogFilePath();
                    _logFilePath = path;
                }

                string? dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string formattedLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(path, formattedLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }

    private static string FormatException(Exception ex)
    {
        var sb = new StringBuilder();
        sb.Append($"{ex.GetType().Name}: {ex.Message}");
        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            sb.Append($"{Environment.NewLine}StackTrace:{Environment.NewLine}{ex.StackTrace}");
        }
        if (ex.InnerException != null)
        {
            sb.Append($"{Environment.NewLine}InnerException: {FormatException(ex.InnerException)}");
        }
        return sb.ToString();
    }

    private static string GetDefaultLogFilePath()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string? projectRoot = null;

            var dir = new DirectoryInfo(baseDir);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "AMEFManager.csproj")))
                {
                    projectRoot = dir.FullName;
                    break;
                }
                dir = dir.Parent;
            }

            if (projectRoot != null)
            {
                return Path.Combine(projectRoot, "debug_log.txt");
            }

            string appDataDir = OperatingSystem.IsMacOS()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "AMEFManager")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AMEFManager");

            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            return Path.Combine(appDataDir, "debug_log.txt");
        }
        catch
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_log.txt");
        }
    }
#endif
}
