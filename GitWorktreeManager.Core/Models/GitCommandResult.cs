namespace GitWorktreeManager.Models;

/// <summary>
/// Represents the result of a Git command execution.
/// </summary>
public record GitCommandResult
{
    /// <summary>
    /// Indicates whether the command executed successfully (exit code 0).
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The error message if the command failed, typically from stderr.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The exit code returned by the Git process.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GitCommandResult Ok() => new()
    {
        Success = true,
        ExitCode = 0
    };

    /// <summary>
    /// Creates a failed result with the specified error message and exit code.
    /// </summary>
    public static GitCommandResult Fail(string errorMessage, int exitCode = -1) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        ExitCode = exitCode
    };
}

/// <summary>
/// Represents the result of a Git command execution that returns data.
/// </summary>
/// <typeparam name="T">The type of data returned by the command.</typeparam>
public record GitCommandResult<T> : GitCommandResult
{
    /// <summary>
    /// The data returned by the command, if successful.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Creates a successful result with the specified data.
    /// </summary>
    public static GitCommandResult<T> Ok(T data) => new()
    {
        Success = true,
        ExitCode = 0,
        Data = data
    };

    /// <summary>
    /// Creates a failed result with the specified error message and exit code.
    /// </summary>
    public new static GitCommandResult<T> Fail(string errorMessage, int exitCode = -1) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        ExitCode = exitCode
    };
}
