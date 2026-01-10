namespace GitWorktreeManager.Services;

/// <summary>
/// Log level enumeration for categorizing log entries.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Informational messages for normal operations.
    /// </summary>
    Information,

    /// <summary>
    /// Warning messages for recoverable issues.
    /// </summary>
    Warning,

    /// <summary>
    /// Error messages for failures.
    /// </summary>
    Error
}

/// <summary>
/// Service interface for logging extension operations.
/// Supports Information, Warning, and Error log levels.
/// </summary>
public interface ILoggerService
{
    /// <summary>
    /// Logs a message at the specified log level.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The message to log.</param>
    public void Log(LogLevel level, string message);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogInformation(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogWarning(string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogError(string message);

    /// <summary>
    /// Logs an exception with full details including stack trace.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Optional additional context message.</param>
    public void LogException(Exception exception, string? message = null);
}
