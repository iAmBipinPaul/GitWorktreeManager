using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Extensibility.UI;

namespace GitWorktreeManager.Dialogs;

/// <summary>
/// Data context for the Add Worktree dialog.
/// Contains the input fields and validation state for creating a new worktree.
/// </summary>
[DataContract]
public class AddWorktreeDialogData : INotifyPropertyChanged
{
    private string _branchName = string.Empty;
    private string _worktreePath = string.Empty;
    private bool _createNewBranch;
    private string? _validationError;
    private bool _isValid;

    /// <summary>
    /// Event raised when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// The branch name for the new worktree.
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
    /// The file system path where the worktree will be created.
    /// </summary>
    [DataMember]
    public string WorktreePath
    {
        get => _worktreePath;
        set
        {
            if (SetProperty(ref _worktreePath, value))
            {
                Validate();
            }
        }
    }

    /// <summary>
    /// If true, creates a new branch with the specified name.
    /// If false, checks out an existing branch.
    /// </summary>
    [DataMember]
    public bool CreateNewBranch
    {
        get => _createNewBranch;
        set => SetProperty(ref _createNewBranch, value);
    }

    /// <summary>
    /// The current validation error message, if any.
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
    /// Indicates whether the current input is valid.
    /// </summary>
    [DataMember]
    public bool IsValid
    {
        get => _isValid;
        private set => SetProperty(ref _isValid, value);
    }

    /// <summary>
    /// Indicates whether there is a validation error to display.
    /// </summary>
    [DataMember]
    public bool HasValidationError => !string.IsNullOrEmpty(ValidationError);

    /// <summary>
    /// Command to browse for a folder location.
    /// </summary>
    [DataMember]
    public IAsyncCommand? BrowseFolderCommand { get; set; }

    /// <summary>
    /// Command to confirm the dialog (OK button).
    /// </summary>
    [DataMember]
    public IAsyncCommand? OkCommand { get; set; }

    /// <summary>
    /// Command to cancel the dialog.
    /// </summary>
    [DataMember]
    public IAsyncCommand? CancelCommand { get; set; }

    /// <summary>
    /// Validates the current input and updates the validation state.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BranchName))
        {
            ValidationError = "Branch name is required.";
            IsValid = false;
            return;
        }

        // Validate branch name format (no spaces, special chars at start)
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

        if (string.IsNullOrWhiteSpace(WorktreePath))
        {
            ValidationError = "Worktree location is required.";
            IsValid = false;
            return;
        }

        // Check for invalid path characters
        var invalidChars = Path.GetInvalidPathChars();
        if (WorktreePath.IndexOfAny(invalidChars) >= 0)
        {
            ValidationError = "Worktree path contains invalid characters.";
            IsValid = false;
            return;
        }

        ValidationError = null;
        IsValid = true;
    }

    /// <summary>
    /// Sets a property value and raises PropertyChanged if the value changed.
    /// </summary>
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

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
