namespace GitWorktreeManager.Services;

using Microsoft.VisualStudio.Extensibility;
using System.Diagnostics;

/// <summary>
/// Logger service implementation that wraps Visual Studio's ActivityLog.
/// Provides Information, Warning, and Error level logging for extension operations.
/// </summary>
public class ActivityLogService : ILoggerService
{
    private const string SourceName = "GitWorktreeManager";
    private readonly TraceSource _traceSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityLogService"/> class.
    /// </summary>
    public ActivityLogService()
    {
        _traceSource = new TraceSource(SourceName, SourceLevels.All);
    }

    /// <inheritdoc />
    public void Log(LogLevel level, string message)
    {
        TraceEventType traceEventType = level switch
        {
            LogLevel.Information => TraceEventType.Information,
            LogLevel.Warning => TraceEventType.Warning,
            LogLevel.Error => TraceEventType.Error,
            _ => TraceEventType.Information
        };

        string formattedMessage = FormatMessage(level, message);
        _traceSource.TraceEvent(traceEventType, 0, formattedMessage);

        // Also write to debug output for development
        Debug.WriteLine(formattedMessage);
    }

    /// <inheritdoc />
    public void LogInformation(string message) => Log(LogLevel.Information, message);

    /// <inheritdoc />
    public void LogWarning(string message) => Log(LogLevel.Warning, message);

    /// <inheritdoc />
    public void LogError(string message) => Log(LogLevel.Error, message);

    /// <inheritdoc />
    public void LogException(Exception exception, string? message = null)
    {
        string fullMessage = BuildExceptionMessage(exception, message);
        Log(LogLevel.Error, fullMessage);
    }

    /// <summary>
    /// Formats a log message with timestamp and source information.
    /// </summary>
    private static string FormatMessage(LogLevel level, string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string levelStr = level switch
        {
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            _ => "UNKNOWN"
        };

        return $"[{timestamp}] [{levelStr}] [{SourceName}] {message}";
    }

    /// <summary>
    /// Builds a comprehensive exception message including stack trace.
    /// </summary>
    private static string BuildExceptionMessage(Exception exception, string? message)
    {
        string fullMessage = string.IsNullOrEmpty(message)
            ? $"Exception: {exception.Message}"
            : $"{message} | Exception: {exception.Message}";

        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            fullMessage += $"\nStackTrace: {exception.StackTrace}";
        }

        if (exception.InnerException != null)
        {
            fullMessage += $"\nInner Exception: {exception.InnerException.Message}";
            if (!string.IsNullOrEmpty(exception.InnerException.StackTrace))
            {
                fullMessage += $"\nInner StackTrace: {exception.InnerException.StackTrace}";
            }
        }

        return fullMessage;
    }
}
