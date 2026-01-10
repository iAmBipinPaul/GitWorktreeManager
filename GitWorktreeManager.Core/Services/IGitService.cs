namespace GitWorktreeManager.Services;

using Models;

public record WorktreeStatus(int ModifiedCount, int UntrackedCount, int Incoming, int Outgoing);

/// <summary>
/// Service interface for executing Git worktree commands.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Gets all worktrees for the specified repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the Git repository.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of worktrees or an error.</returns>
    public Task<GitCommandResult<IReadOnlyList<Worktree>>> GetWorktreesAsync(
        string repositoryPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new worktree to the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the Git repository.</param>
    /// <param name="worktreePath">The path where the new worktree will be created.</param>
    /// <param name="branchName">The branch name to checkout in the worktree.</param>
    /// <param name="createBranch">If true, creates a new branch with the -b flag.</param>
    /// <param name="baseBranch">The base branch to create the new branch from (only used when createBranch is true).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<GitCommandResult> AddWorktreeAsync(
        string repositoryPath,
        string worktreePath,
        string branchName,
        bool createBranch = false,
        string? baseBranch = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a worktree from the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the Git repository.</param>
    /// <param name="worktreePath">The path of the worktree to remove.</param>
    /// <param name="force">If true, forces removal even with uncommitted changes.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Task<GitCommandResult> RemoveWorktreeAsync(
        string repositoryPath,
        string worktreePath,
        bool force = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the root directory of the Git repository containing the specified path.
    /// </summary>
    /// <param name="path">A path within the repository.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The repository root path, or null if not a Git repository.</returns>
    public Task<string?> GetRepositoryRootAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if Git is installed and available in the system PATH.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if Git is installed and accessible, false otherwise.</returns>
    public Task<bool> IsGitInstalledAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all branches (local and remote) for the specified repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the Git repository.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of branch names or an error.</returns>
    public Task<GitCommandResult<IReadOnlyList<string>>> GetBranchesAsync(
        string repositoryPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a worktree (changes, ahead/behind counts).
    /// </summary>
    /// <param name="worktreePath">The path to the worktree.</param>
    /// <param name="branchName">The branch name.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The status of the worktree.</returns>
    public Task<WorktreeStatus> GetWorktreeStatusAsync(string worktreePath, string branchName,
        CancellationToken cancellationToken = default);
}
