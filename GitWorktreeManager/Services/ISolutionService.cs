namespace GitWorktreeManager.Services;

/// <summary>
/// Service interface for monitoring Visual Studio solution events.
/// </summary>
public interface ISolutionService : IDisposable
{
    /// <summary>
    /// Event raised when the solution changes (opened, closed, or switched).
    /// The event is debounced to prevent excessive refreshes.
    /// </summary>
    event EventHandler<SolutionChangedEventArgs>? SolutionChanged;

    /// <summary>
    /// Gets the current solution directory path, or null if no solution is open.
    /// </summary>
    string? CurrentSolutionDirectory { get; }

    /// <summary>
    /// Initializes the solution service and starts monitoring for solution events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for solution change events.
/// </summary>
public class SolutionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the path to the solution directory, or null if no solution is open.
    /// </summary>
    public string? SolutionDirectory { get; }

    /// <summary>
    /// Gets a value indicating whether a solution is currently open.
    /// </summary>
    public bool IsSolutionOpen => !string.IsNullOrEmpty(SolutionDirectory);

    /// <summary>
    /// Initializes a new instance of the <see cref="SolutionChangedEventArgs"/> class.
    /// </summary>
    /// <param name="solutionDirectory">The solution directory path, or null if closed.</param>
    public SolutionChangedEventArgs(string? solutionDirectory)
    {
        SolutionDirectory = solutionDirectory;
    }
}
