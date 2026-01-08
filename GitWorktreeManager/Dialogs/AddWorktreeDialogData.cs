using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace GitWorktreeManager.Dialogs;

/// <summary>
/// Data context for the Add Worktree dialog.
/// Two modes:
/// - Create new branch: Enter branch name + select base branch
/// - Use existing branch: Select branch from dropdown
/// </summary>
[DataContract]
public class AddWorktreeDialogData : INotifyPropertyChanged
{
    private string _branchName = string.Empty;
    private string? _selectedBranch;
    private bool _createNewBranch = true;
    private string? _validationError;
    private bool _isValid;
    private string _repositoryPath = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Available branches to select.
    /// </summary>
    [DataMember]
    public ObservableCollection<string> AvailableBranches { get; } = new();

    /// <summary>
    /// The branch name for the new worktree (only used when CreateNewBranch is true).
    /// </summary>
    [DataMember]
    public string BranchName
    {
        get => _branchName;
        set
        {
            if (SetProperty(ref _branchName, value))
            {
                Validate();
            }
        }
    }

    /// <summary>
    /// The selected branch from dropdown.
    /// When CreateNewBranch=true: this is the base branch for the new branch.
    /// When CreateNewBranch=false: this is the existing branch to checkout.
    /// </summary>
    [DataMember]
    public string? SelectedBranch
    {
        get => _selectedBranch;
        set
        {
            if (SetProperty(ref _selectedBranch, value))
            {
                Validate();
            }
        }
    }

    /// <summary>
    /// If true, creates a new branch. If false, uses existing branch.
    /// </summary>
    [DataMember]
    public bool CreateNewBranch
    {
        get => _createNewBranch;
        set
        {
            if (SetProperty(ref _createNewBranch, value))
            {
                OnPropertyChanged(nameof(BranchSelectorLabel));
                OnPropertyChanged(nameof(BranchSelectorTooltip));
                Validate();
            }
        }
    }

    /// <summary>
    /// Dynamic label for the branch selector dropdown.
    /// </summary>
    [DataMember]
    public string BranchSelectorLabel => CreateNewBranch ? "Based on:" : "Branch:";

    /// <summary>
    /// Dynamic tooltip for the branch selector dropdown.
    /// </summary>
    [DataMember]
    public string BranchSelectorTooltip => CreateNewBranch 
        ? "Select the branch to base your new branch on" 
        : "Select an existing branch to checkout";

    /// <summary>
    /// Repository path for generating worktree location.
    /// </summary>
    [DataMember]
    public string RepositoryPath
    {
        get => _repositoryPath;
        set => SetProperty(ref _repositoryPath, value);
    }

    /// <summary>
    /// Validation error message.
    /// </summary>
    [DataMember]
    public string? ValidationError
    {
        get => _validationError;
        private set
        {
            if (SetProperty(ref _validationError, value))
            {
                OnPropertyChanged(nameof(HasValidationError));
            }
        }
    }

    /// <summary>
    /// Whether the input is valid.
    /// </summary>
    [DataMember]
    public bool IsValid
    {
        get => _isValid;
        private set => SetProperty(ref _isValid, value);
    }

    /// <summary>
    /// Whether there's a validation error.
    /// </summary>
    [DataMember]
    public bool HasValidationError => !string.IsNullOrEmpty(ValidationError);

    /// <summary>
    /// OK command.
    /// </summary>
    [DataMember]
    public IAsyncCommand? OkCommand { get; set; }

    /// <summary>
    /// Cancel command.
    /// </summary>
    [DataMember]
    public IAsyncCommand? CancelCommand { get; set; }

    /// <summary>
    /// Generates the worktree path based on branch name.
    /// </summary>
    public string GetWorktreePath()
    {
        if (string.IsNullOrWhiteSpace(RepositoryPath))
        {
            return string.Empty;
        }

        // Use BranchName when creating new branch, SelectedBranch when using existing
        var branchForPath = CreateNewBranch ? BranchName : SelectedBranch;
        
        if (string.IsNullOrWhiteSpace(branchForPath))
        {
            return string.Empty;
        }

        var repoParent = Path.GetDirectoryName(RepositoryPath);
        if (string.IsNullOrEmpty(repoParent))
        {
            return string.Empty;
        }

        var repoName = Path.GetFileName(RepositoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var safeBranchName = SanitizeBranchName(branchForPath);

        return Path.Combine(repoParent, $"{repoName}-{safeBranchName}");
    }

    /// <summary>
    /// Gets the effective branch name for the worktree.
    /// When creating new branch: returns BranchName.
    /// When using existing: returns SelectedBranch.
    /// </summary>
    public string GetEffectiveBranchName()
    {
        return CreateNewBranch ? BranchName : (SelectedBranch ?? string.Empty);
    }

    private static string SanitizeBranchName(string branchName)
    {
        return branchName
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace(":", "-")
            .Replace("*", "-")
            .Replace("?", "-")
            .Replace("\"", "-")
            .Replace("<", "-")
            .Replace(">", "-")
            .Replace("|", "-");
    }

    /// <summary>
    /// Validates the input based on the current mode.
    /// </summary>
    public void Validate()
    {
        if (CreateNewBranch)
        {
            // Creating new branch: need branch name + base branch
            if (string.IsNullOrWhiteSpace(BranchName))
            {
                ValidationError = "Branch name is required.";
                IsValid = false;
                return;
            }

            if (BranchName.Contains(' '))
            {
                ValidationError = "Branch name cannot contain spaces.";
                IsValid = false;
                return;
            }

            if (BranchName.StartsWith("-") || BranchName.StartsWith("."))
            {
                ValidationError = "Branch name cannot start with '-' or '.'.";
                IsValid = false;
                return;
            }

            if (string.IsNullOrEmpty(SelectedBranch))
            {
                ValidationError = "Please select a base branch.";
                IsValid = false;
                return;
            }
        }
        else
        {
            // Using existing branch: just need selected branch
            if (string.IsNullOrEmpty(SelectedBranch))
            {
                ValidationError = "Please select a branch.";
                IsValid = false;
                return;
            }
        }

        ValidationError = null;
        IsValid = true;
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
