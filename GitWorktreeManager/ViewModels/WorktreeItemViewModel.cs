using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace GitWorktreeManager.ViewModels;

/// <summary>
/// ViewModel representing a single worktree item for display in the UI.
/// Uses DataContract attributes for Remote UI serialization.
/// </summary>
[DataContract]
public class WorktreeItemViewModel : INotifyPropertyChanged
{
    private bool _isLoadingStatus;
    private bool _hasUncommittedChanges;
    private int _uncommittedChangesCount;
    private int _untrackedChangesCount;
    private int _incomingCommits;
    private int _outgoingCommits;
    private string? _statusSummary;

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

    /// <summary>
    /// Indicates if status is currently being fetched
    /// </summary>
    [DataMember]
    public bool IsLoadingStatus
    {
        get => _isLoadingStatus;
        set => SetProperty(ref _isLoadingStatus, value);
    }

    /// <summary>
    /// True if there are modified/staged files
    /// </summary>
    [DataMember]
    public bool HasUncommittedChanges
    {
        get => _hasUncommittedChanges;
        set => SetProperty(ref _hasUncommittedChanges, value);
    }

    /// <summary>
    /// Count of modified/staged files
    /// </summary>
    [DataMember]
    public int UncommittedChangesCount
    {
        get => _uncommittedChangesCount;
        set => SetProperty(ref _uncommittedChangesCount, value);
    }

    /// <summary>
    /// True if there are untracked files
    /// </summary>
    [DataMember]
    public bool HasUntrackedChanges => UntrackedChangesCount > 0;

    /// <summary>
    /// Count of untracked files
    /// </summary>
    [DataMember]
    public int UntrackedChangesCount
    {
        get => _untrackedChangesCount;
        set
        {
            if (SetProperty(ref _untrackedChangesCount, value))
            {
                OnPropertyChanged(nameof(HasUntrackedChanges));
            }
        }
    }

    /// <summary>
    /// Number of commits incoming from upstream
    /// </summary>
    [DataMember]
    public int IncomingCommits
    {
        get => _incomingCommits;
        set => SetProperty(ref _incomingCommits, value);
    }

    /// <summary>
    /// Number of commits waiting to be pushed
    /// </summary>
    [DataMember]
    public int OutgoingCommits
    {
        get => _outgoingCommits;
        set => SetProperty(ref _outgoingCommits, value);
    }

    /// <summary>
    /// Tooltip friendly summary (e.g. "3 modified, 2 ahead, 1 behind")
    /// </summary>
    [DataMember]
    public string? StatusSummary
    {
        get => _statusSummary;
        set => SetProperty(ref _statusSummary, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
