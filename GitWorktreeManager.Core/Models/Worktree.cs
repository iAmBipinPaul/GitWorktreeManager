namespace GitWorktreeManager.Models;

/// <summary>
/// Represents a Git worktree with its associated metadata.
/// Parsed from the output of 'git worktree list --porcelain'.
/// </summary>
public record Worktree
{
    /// <summary>
    /// The absolute path to the worktree directory.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The SHA of the HEAD commit in this worktree.
    /// </summary>
    public required string HeadCommit { get; init; }

    /// <summary>
    /// The branch name (without refs/heads/ prefix), or null if in detached HEAD state.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Indicates whether this is the main (bare) worktree.
    /// The first worktree in the list is always the main worktree.
    /// </summary>
    public bool IsMainWorktree { get; init; }

    /// <summary>
    /// Indicates whether this worktree is locked.
    /// </summary>
    public bool IsLocked { get; init; }

    /// <summary>
    /// The reason for locking, if the worktree is locked.
    /// </summary>
    public string? LockReason { get; init; }

    /// <summary>
    /// Indicates whether this worktree can be pruned.
    /// </summary>
    public bool IsPrunable { get; init; }

    /// <summary>
    /// Indicates whether this worktree is in detached HEAD state.
    /// </summary>
    public bool IsDetached => Branch is null;
}
