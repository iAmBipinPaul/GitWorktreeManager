namespace GitWorktreeManager.Services;

/// <summary>
/// A simple console-based logger implementation for use in testing and non-VS contexts.
/// </summary>
public class ConsoleLoggerService : ILoggerService
{
    private const string SourceName = "GitWorktreeManager";

    /// <inheritdoc />
    public void Log(LogLevel level, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level switch
        {
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            _ => "UNKNOWN"
        };

        Console.WriteLine($"[{timestamp}] [{levelStr}] [{SourceName}] {message}");
    }

    /// <inheritdoc />
    public void LogInformation(string message)
    {
        Log(LogLevel.Information, message);
    }

    /// <inheritdoc />
    public void LogWarning(string message)
    {
        Log(LogLevel.Warning, message);
    }

    /// <inheritdoc />
    public void LogError(string message)
    {
        Log(LogLevel.Error, message);
    }

    /// <inheritdoc />
    public void LogException(Exception exception, string? message = null)
    {
        var fullMessage = string.IsNullOrEmpty(message)
            ? $"Exception: {exception.Message}\nStackTrace: {exception.StackTrace}"
            : $"{message}\nException: {exception.Message}\nStackTrace: {exception.StackTrace}";

        if (exception.InnerException != null)
        {
            fullMessage += $"\nInner Exception: {exception.InnerException.Message}\nInner StackTrace: {exception.InnerException.StackTrace}";
        }

        Log(LogLevel.Error, fullMessage);
    }
}
