using System.Text;

namespace Kogl.Core;

/// <summary>
/// The log level
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// The trace level
    /// </summary>
    Trace,

    /// <summary>
    /// The debug level
    /// </summary>
    Debug,

    /// <summary>
    /// The info level
    /// </summary>
    Info,

    /// <summary>
    /// The warning level
    /// </summary>
    Warn,

    /// <summary>
    /// The error level
    /// </summary>
    Error,

    /// <summary>
    /// The critical level
    /// </summary>
    Critical,
}

/// <summary>
/// A log entry
/// </summary>
/// <param name="Timestamp">The timestamp</param>
/// <param name="Level">The level</param>
/// <param name="Category">The category</param>
/// <param name="Message">The message</param>
/// <param name="ThreadId">The thread ID</param>
public readonly record struct LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    int ThreadId
);

/// <summary>
/// A logger
/// </summary>
public static class Log
{
    private static readonly Lock _lock = new();
    private static readonly List<LogEntry> _history = [];
    private static StreamWriter? _fileWriter;
    private static LogLevel _minLogLevel = LogLevel.Trace;
    private static bool _enableFileLogging = false;

    // config settings
    public static LogLevel MinLogLevel => _minLogLevel;
    public static int MaxHistoryItems { get; set; } = 2000;
    public static bool EnableConsoleColors { get; set; } = true;
    public static bool EnableFileLogging => _enableFileLogging;

    // triggered whenever a new log arrives (useful for real-time imgui auto-scroll)
    public static event Action<LogEntry>? OnLogReceived;

    // ANSI escape codes
    private const string _reset = "\u001b[0m";
    private const string _gray = "\u001b[90m";
    private const string _cyan = "\u001b[36m";
    private const string _green = "\u001b[32m";
    private const string _yellow = "\u001b[33m";
    private const string _red = "\u001b[31m";
    private const string _boldRed = "\u001b[1;31m";

    /// <summary>
    /// Sets the minimum log level required to process a log entry.
    /// </summary>
    /// <param name="level">The log level threshold.</param>
    public static void SetMinLogLevel(LogLevel level)
    {
        _minLogLevel = level;
    }

    /// <summary>
    /// Enables or disables file logging.
    /// </summary>
    /// <param name="enabled">True to enable file logging, false to disable.</param>
    public static void SetFileLogging(bool enabled)
    {
        _enableFileLogging = enabled;
    }

    /// <summary>
    /// Initializes the file logger.
    /// </summary>
    public static void InitFileLogger()
    {
        try
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // session-based unique log name: logs/log_2026-05-15_23-26-06.txt
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filePath = Path.Combine(logDirectory, $"log_{timestamp}.txt");

            // open stream with share access so you can view it live while the engine runs
            FileStream fileStream = new(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read
            );
            _fileWriter = new StreamWriter(fileStream, Encoding.UTF8) { AutoFlush = true };

            Info("CORE", $"Logging to file (if enabled): {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] [CORE] Failed to initialize file logger: {ex.Message}");
        }
    }

    /// <summary>
    /// Core thread-safe logging pipeline
    /// </summary>
    public static void Write(LogLevel level, string category, string message)
    {
#if DEBUG
        if (level < MinLogLevel)
            return;

        LogEntry entry = new(
            DateTime.Now,
            level,
            category.ToUpperUnchecked(),
            message,
            Environment.CurrentManagedThreadId
        );

        lock (_lock)
        {
            // add to ImGui history buffer
            _history.Add(entry);
            if (_history.Count > MaxHistoryItems)
            {
                _history.RemoveAt(0); // maintain fixed memory footprint
            }

            // format and write to terminal console
            string consoleLine = FormatConsole(entry);
            Console.Out.WriteLine(consoleLine);

            // format and write to persistent log file (stripping ANSI codes)
            if (_fileWriter != null && EnableFileLogging)
            {
                string fileLine =
                    $"[{entry.Timestamp:HH:mm:ss}] [{entry.Level.ToString().ToUpper()}] [{entry.Category}] [Thread {entry.ThreadId}] {entry.Message}";
                _fileWriter.WriteLine(fileLine);
            }
        }

        // invoke event outside lock to prevent deadlock scenarios in UI threads
        OnLogReceived?.Invoke(entry);
#endif
    }

    private static string ToUpperUnchecked(this string str)
    {
        return str.ToUpperInvariant();
    }

    private static string FormatConsole(LogEntry entry)
    {
        if (!EnableConsoleColors)
        {
            return $"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] [{entry.Category}] {entry.Message}";
        }

        string color = entry.Level switch
        {
            LogLevel.Trace => _gray,
            LogLevel.Debug => _cyan,
            LogLevel.Info => _green,
            LogLevel.Warn => _yellow,
            LogLevel.Error => _red,
            LogLevel.Critical => _boldRed,
            _ => _reset,
        };

        return $"{_gray}[{entry.Timestamp:HH:mm:ss}]{_reset} {color}[{entry.Level.ToString().ToUpper()}]{_reset} {_gray}[{entry.Category}]{_reset} {entry.Message}";
    }

    #region Helper Overloads

    public static void Trace(string category, string message)
    {
        Write(LogLevel.Trace, category, message);
    }

    public static void Debug(string category, string message)
    {
        Write(LogLevel.Debug, category, message);
    }

    public static void Info(string category, string message)
    {
        Write(LogLevel.Info, category, message);
    }

    public static void Warn(string category, string message)
    {
        Write(LogLevel.Warn, category, message);
    }

    public static void Error(string category, string message)
    {
        Write(LogLevel.Error, category, message);
    }

    public static void Critical(string category, string message)
    {
        Write(LogLevel.Critical, category, message);
    }

    // =========================================================================
    // context-free defaults
    // =========================================================================

    public static void Trace(string message)
    {
        Write(LogLevel.Trace, "ENGINE", message);
    }

    public static void Debug(string message)
    {
        Write(LogLevel.Debug, "ENGINE", message);
    }

    public static void Info(string message)
    {
        Write(LogLevel.Info, "ENGINE", message);
    }

    public static void Warn(string message)
    {
        Write(LogLevel.Warn, "ENGINE", message);
    }

    public static void Error(string message)
    {
        Write(LogLevel.Error, "ENGINE", message);
    }

    public static void Critical(string message)
    {
        Write(LogLevel.Critical, "ENGINE", message);
    }

    // =========================================================================
    // formatted string support
    // =========================================================================

    public static void Info(string category, string format, params object?[] args)
    {
        Write(LogLevel.Info, category, string.Format(format, args));
    }

    public static void Warn(string category, string format, params object?[] args)
    {
        Write(LogLevel.Warn, category, string.Format(format, args));
    }

    public static void Error(string category, string format, params object?[] args)
    {
        Write(LogLevel.Error, category, string.Format(format, args));
    }

    // exception tracking support
    public static void Exception(
        string category,
        Exception ex,
        string contextMessage = "An unhandled exception occurred."
    )
    {
        Write(
            LogLevel.Error,
            category,
            $"{contextMessage} Exception: {ex.Message}\nStack Trace:\n{ex.StackTrace}"
        );
    }

    #endregion
    #region ImGui Integration API

    /// <summary>
    /// Returns a snapshot copy of the current log history in a thread-safe manner
    /// </summary>
    public static LogEntry[] GetHistorySnapshot()
    {
        lock (_lock)
        {
            return [.. _history];
        }
    }

    /// <summary>
    /// Clears the in-memory log queue.
    /// </summary>
    public static void ClearHistory()
    {
        lock (_lock)
        {
            _history.Clear();
        }
    }

    /// <summary>
    /// Ensures all logs are completely flushed and the session file handle is safely freed.
    /// </summary>
    public static void Shutdown()
    {
        lock (_lock)
        {
            if (_fileWriter != null)
            {
                Info("CORE", "Closing session logger safely.");
                _fileWriter.Flush();
                _fileWriter.Dispose();
                _fileWriter = null;
            }
        }
    }

    #endregion
}
