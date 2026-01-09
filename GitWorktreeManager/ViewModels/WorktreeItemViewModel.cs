using System.Runtime.Serialization;

namespace GitWorktreeManager.ViewModels;

/// <summary>
/// ViewModel representing a single worktree item for display in the UI.
/// Uses DataContract attributes for Remote UI serialization.
/// </summary>
[DataContract]
public class WorktreeItemViewModel
{
    /// <summary>
    /// The absolute path to the worktree directory.
    /// </summary>
    [DataMember]
    public required string Path { get; init; }

    /// <summary>
    /// A shortened display path for the UI (e.g., folder name or relative path).
    /// </summary>
    [DataMember]
    public required string DisplayPath { get; init; }

    /// <summary>
    /// The branch name, or "detached" if in detached HEAD state.
    /// </summary>
    [DataMember]
    public required string BranchName { get; init; }

    /// <summary>
    /// The abbreviated HEAD commit SHA for display.
    /// </summary>
    [DataMember]
    public required string HeadCommit { get; init; }

    /// <summary>
    /// Indicates whether this is the main (bare) worktree.
    /// </summary>
    [DataMember]
    public bool IsMainWorktree { get; init; }

    /// <summary>
    /// Indicates whether this worktree is locked.
    /// </summary>
    [DataMember]
    public bool IsLocked { get; init; }

    /// <summary>
    /// Indicates whether this worktree can be pruned.
    /// </summary>
    [DataMember]
    public bool IsPrunable { get; init; }

    /// <summary>
    /// Indicates whether this is the currently open worktree in VS.
    /// </summary>
    [DataMember]
    public bool IsCurrentWorktree { get; set; }

    /// <summary>
    /// Indicates whether this worktree can be removed.
    /// Main worktrees and current worktree cannot be removed.
    /// </summary>
    [DataMember]
    public bool CanRemove => !IsMainWorktree && !IsCurrentWorktree;
}
