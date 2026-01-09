using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using GitWorktreeManager.Dialogs;
using GitWorktreeManager.Services;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

namespace GitWorktreeManager.ViewModels;

/// <summary>
/// ViewModel for the Worktree Manager tool window.
/// Manages the collection of worktrees and exposes commands for worktree operations.
/// Uses DataContract attributes for Remote UI serialization.
/// </summary>
[DataContract]
public class WorktreeViewModel : INotifyPropertyChanged
{
    private readonly IGitService _gitService;
    private readonly VisualStudioExtensibility? _extensibility;
    private readonly INotificationService? _notificationService;
    private bool _isLoading;
    private bool _hasRepository;
    private string? _errorMessage;
    private string? _repositoryPath;
    private string? _successMessage;
    private string? _searchFilter;
    private ObservableCollection<WorktreeItemViewModel> _filteredWorktrees;

    /// <summary>
    /// Initializes a new instance of the WorktreeViewModel.
    /// </summary>
    /// <param name="gitService">The Git service for executing commands.</param>
    /// <param name="extensibility">The VS extensibility object for showing dialogs.</param>
    /// <param name="notificationService">The notification service for displaying messages.</param>
    public WorktreeViewModel(
        IGitService gitService, 
        VisualStudioExtensibility? extensibility = null,
        INotificationService? notificationService = null)
    {
        _gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
        _extensibility = extensibility;
        _notificationService = notificationService;
        Worktrees = new ObservableCollection<WorktreeItemViewModel>();
        _filteredWorktrees = new ObservableCollection<WorktreeItemViewModel>();
        
        // Initialize commands
        RefreshCommand = new AsyncCommand(async (_, ct) => await RefreshAsync(ct));
        AddWorktreeCommand = new AsyncCommand(async (_, ct) => await OnAddWorktreeCommandAsync(ct));
        RemoveWorktreeCommand = new AsyncCommand(async (param, ct) => 
        {
            // Debug: Log what we received
            System.Diagnostics.Debug.WriteLine($"RemoveWorktreeCommand received param type: {param?.GetType().Name ?? "null"}");
            
            if (param is WorktreeItemViewModel item)
            {
                await RemoveWorktreeAsync(item, force: false, ct);
            }
            else
            {
                // Parameter wasn't the expected type - show error
                var errorMsg = $"Remove command received unexpected parameter: {param?.GetType().Name ?? "null"}";
                System.Diagnostics.Debug.WriteLine(errorMsg);
                await ShowErrorNotificationAsync("Failed to remove worktree - invalid selection", ct);
            }
        });
        OpenInNewWindowCommand = new AsyncCommand(async (param, ct) =>
        {
            if (param is WorktreeItemViewModel item)
            {
                await OpenInNewWindowAsync(item, ct);
            }
        });
        OpenInExplorerCommand = new AsyncCommand(async (param, ct) =>
        {
            System.Diagnostics.Debug.WriteLine($"OpenInExplorerCommand param: {param?.GetType().Name ?? "null"}");
            if (param is WorktreeItemViewModel item)
            {
                System.Diagnostics.Debug.WriteLine($"OpenInExplorerCommand path: {item.Path}");
                await OpenInExplorerAsync(item, ct);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"OpenInExplorerCommand: param is not WorktreeItemViewModel");
            }
        });
        CopyPathCommand = new AsyncCommand(async (param, ct) =>
        {
            if (param is WorktreeItemViewModel item)
            {
                await CopyPathToClipboardAsync(item, ct);
            }
        });
        DismissErrorCommand = new AsyncCommand((_, ct) =>
        {
            ErrorMessage = null;
            return Task.CompletedTask;
        });
        DismissSuccessCommand = new AsyncCommand((_, ct) =>
        {
            SuccessMessage = null;
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Command to refresh the worktree list.
    /// </summary>
    [DataMember]
    public IAsyncCommand RefreshCommand { get; }

    /// <summary>
    /// Command to add a new worktree.
    /// </summary>
    [DataMember]
    public IAsyncCommand AddWorktreeCommand { get; }

    /// <summary>
    /// Command to remove a worktree.
    /// </summary>
    [DataMember]
    public IAsyncCommand RemoveWorktreeCommand { get; }

    /// <summary>
    /// Command to open a worktree in a new Visual Studio window.
    /// </summary>
    [DataMember]
    public IAsyncCommand OpenInNewWindowCommand { get; }

    /// <summary>
    /// Command to open a worktree folder in Windows Explorer.
    /// </summary>
    [DataMember]
    public IAsyncCommand OpenInExplorerCommand { get; }

    /// <summary>
    /// Command to copy a worktree path to clipboard.
    /// </summary>
    [DataMember]
    public IAsyncCommand CopyPathCommand { get; }

    /// <summary>
    /// Command to dismiss the error message.
    /// </summary>
    [DataMember]
    public IAsyncCommand DismissErrorCommand { get; }

    /// <summary>
    /// Command to dismiss the success message.
    /// </summary>
    [DataMember]
    public IAsyncCommand DismissSuccessCommand { get; }

    /// <summary>
    /// Handles the Add Worktree command - shows dialog and creates worktree.
    /// </summary>
    private async Task OnAddWorktreeCommandAsync(CancellationToken cancellationToken)
    {
        if (_extensibility == null)
        {
            ErrorMessage = "Cannot show dialog: extensibility not available.";
            return;
        }

        if (string.IsNullOrEmpty(RepositoryPath))
        {
            ErrorMessage = "No repository is currently open.";
            await ShowErrorNotificationAsync("No repository is currently open.", cancellationToken);
            return;
        }

        // Clear any previous messages
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            // Show the Add Worktree dialog with GitService for branch loading
            var dialog = new AddWorktreeDialog(_extensibility, _gitService);
            var result = await dialog.ShowAsync(RepositoryPath, cancellationToken);

            if (result == null)
            {
                // User cancelled
                return;
            }

            // Validate inputs
            if (string.IsNullOrWhiteSpace(result.BranchName))
            {
                ErrorMessage = "Branch name cannot be empty.";
                await ShowErrorNotificationAsync("Branch name cannot be empty.", cancellationToken);
                return;
            }

            if (string.IsNullOrWhiteSpace(result.WorktreePath))
            {
                ErrorMessage = "Worktree path cannot be empty.";
                await ShowErrorNotificationAsync("Worktree path cannot be empty.", cancellationToken);
                return;
            }

            // Add the worktree
            var addResult = await AddWorktreeAsync(
                result.WorktreePath,
                result.BranchName,
                result.CreateNewBranch,
                result.BaseBranch,
                cancellationToken);

            if (addResult.Success)
            {
                SuccessMessage = $"Worktree '{result.BranchName}' created successfully!";
                
                // Open in new VS window if requested
                if (result.OpenAfterCreation)
                {
                    var worktreeItem = new WorktreeItemViewModel
                    {
                        Path = result.WorktreePath,
                        DisplayPath = System.IO.Path.GetFileName(result.WorktreePath),
                        BranchName = result.BranchName,
                        HeadCommit = string.Empty,
                        IsMainWorktree = false,
                        IsLocked = false,
                        IsPrunable = false
                    };
                    await OpenInNewWindowAsync(worktreeItem, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error adding worktree: {ex.Message}";
            ErrorMessage = errorMsg;
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
        }
    }

    /// <summary>
    /// Event raised when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// The collection of worktrees to display in the UI.
    /// </summary>
    [DataMember]
    public ObservableCollection<WorktreeItemViewModel> Worktrees { get; }

    /// <summary>
    /// Indicates whether a loading operation is in progress.
    /// </summary>
    [DataMember]
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(ShowNoRepositoryMessage));
            }
        }
    }

    /// <summary>
    /// Indicates whether a Git repository is detected.
    /// </summary>
    [DataMember]
    public bool HasRepository
    {
        get => _hasRepository;
        private set
        {
            if (SetProperty(ref _hasRepository, value))
            {
                OnPropertyChanged(nameof(ShowNoRepositoryMessage));
            }
        }
    }

    /// <summary>
    /// The current error message, if any.
    /// </summary>
    [DataMember]
    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>
    /// The current success message, if any.
    /// </summary>
    [DataMember]
    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            if (SetProperty(ref _successMessage, value))
            {
                OnPropertyChanged(nameof(HasSuccess));
            }
        }
    }

    /// <summary>
    /// The path to the current Git repository.
    /// </summary>
    [DataMember]
    public string? RepositoryPath
    {
        get => _repositoryPath;
        set => SetProperty(ref _repositoryPath, value);
    }

    /// <summary>
    /// The search filter text for filtering worktrees.
    /// </summary>
    [DataMember]
    public string? SearchFilter
    {
        get => _searchFilter;
        set
        {
            if (SetProperty(ref _searchFilter, value))
            {
                UpdateFilteredWorktrees();
            }
        }
    }

    /// <summary>
    /// The filtered collection of worktrees based on search filter.
    /// </summary>
    [DataMember]
    public ObservableCollection<WorktreeItemViewModel> FilteredWorktrees
    {
        get => _filteredWorktrees;
        private set => SetProperty(ref _filteredWorktrees, value);
    }

    /// <summary>
    /// Indicates whether the "No Repository" message should be shown.
    /// True when not loading and no repository is detected.
    /// </summary>
    [DataMember]
    public bool ShowNoRepositoryMessage => !IsLoading && !HasRepository;

    /// <summary>
    /// Indicates whether the "No Worktrees" message should be shown.
    /// True when has repository, not loading, and no filtered worktrees.
    /// </summary>
    [DataMember]
    public bool ShowNoWorktreesMessage => HasRepository && !IsLoading && FilteredWorktrees.Count == 0;

    /// <summary>
    /// Indicates whether the worktree list should be shown.
    /// True when has repository and has filtered worktrees.
    /// </summary>
    [DataMember]
    public bool ShowWorktreeList => HasRepository && FilteredWorktrees.Count > 0;

    /// <summary>
    /// Indicates whether there is an error message to display.
    /// </summary>
    [DataMember]
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Indicates whether there is a success message to display.
    /// </summary>
    [DataMember]
    public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

    /// <summary>
    /// Indicates whether Git is not installed.
    /// </summary>
    [DataMember]
    public bool IsGitNotInstalled { get; private set; }

    /// <summary>
    /// Sets the error state when Git is not installed.
    /// </summary>
    public void SetGitNotInstalledError()
    {
        IsGitNotInstalled = true;
        HasRepository = false;
        ErrorMessage = "Git is not installed or not found in PATH. Please install Git and ensure it is added to your system PATH.";
        OnPropertyChanged(nameof(IsGitNotInstalled));
    }


    /// <summary>
    /// Refreshes the worktree list from the Git repository.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Don't refresh if Git is not installed
        if (IsGitNotInstalled)
        {
            return;
        }

        if (string.IsNullOrEmpty(RepositoryPath))
        {
            HasRepository = false;
            Worktrees.Clear();
            FilteredWorktrees.Clear();
            ErrorMessage = null;
            OnPropertyChanged(nameof(ShowNoWorktreesMessage));
            OnPropertyChanged(nameof(ShowWorktreeList));
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _gitService.GetWorktreesAsync(RepositoryPath, cancellationToken);

            if (result.Success && result.Data != null)
            {
                HasRepository = true;
                Worktrees.Clear();

                foreach (var worktree in result.Data)
                {
                    var viewModel = MapToViewModel(worktree);
                    Worktrees.Add(viewModel);
                }
                
                UpdateFilteredWorktrees();
            }
            else
            {
                HasRepository = false;
                var errorMsg = result.ErrorMessage ?? "Failed to retrieve worktrees";
                ErrorMessage = errorMsg;
                Worktrees.Clear();
                FilteredWorktrees.Clear();
                OnPropertyChanged(nameof(ShowNoWorktreesMessage));
                OnPropertyChanged(nameof(ShowWorktreeList));
                
                // Show notification for git errors (includes stderr)
                await ShowErrorNotificationAsync("Failed to retrieve worktrees", result.ErrorMessage, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            HasRepository = false;
            var errorMsg = $"Error refreshing worktrees: {ex.Message}";
            ErrorMessage = errorMsg;
            Worktrees.Clear();
            FilteredWorktrees.Clear();
            OnPropertyChanged(nameof(ShowNoWorktreesMessage));
            OnPropertyChanged(nameof(ShowWorktreeList));
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Opens a worktree in a new Visual Studio instance.
    /// </summary>
    /// <param name="worktreeItem">The worktree item to open.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure with an error message.</returns>
    public async Task<(bool Success, string? ErrorMessage)> OpenInNewWindowAsync(
        WorktreeItemViewModel worktreeItem,
        CancellationToken cancellationToken = default)
    {
        if (worktreeItem == null)
        {
            var errorMsg = "No worktree selected";
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }

        if (!System.IO.Directory.Exists(worktreeItem.Path))
        {
            var errorMsg = $"Worktree path does not exist: {worktreeItem.Path}";
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }

        try
        {
            // Find solution files in the worktree path
            var solutionFiles = System.IO.Directory.GetFiles(
                worktreeItem.Path,
                "*.sln",
                System.IO.SearchOption.TopDirectoryOnly);

            string argument;

            if (solutionFiles.Length == 1)
            {
                // Single solution file - open it directly
                argument = $"\"{solutionFiles[0]}\"";
            }
            else if (solutionFiles.Length > 1)
            {
                // Multiple solution files - open the folder and let user choose
                argument = $"\"{worktreeItem.Path}\"";
            }
            else
            {
                // No solution files - open the folder
                argument = $"\"{worktreeItem.Path}\"";
            }

            // Launch devenv.exe with the appropriate argument
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "devenv.exe",
                Arguments = argument,
                UseShellExecute = true
            };

            await Task.Run(() =>
            {
                System.Diagnostics.Process.Start(startInfo);
            }, cancellationToken);

            return (true, null);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error opening worktree in new window: {ex.Message}";
            ErrorMessage = errorMsg;
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }
    }

    /// <summary>
    /// Removes a worktree from the repository.
    /// </summary>
    /// <param name="worktreeItem">The worktree item to remove.</param>
    /// <param name="force">If true, forces removal even with uncommitted changes.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure with an error message.</returns>
    public async Task<(bool Success, string? ErrorMessage)> RemoveWorktreeAsync(
        WorktreeItemViewModel worktreeItem,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        System.Diagnostics.Debug.WriteLine($"RemoveWorktreeAsync called for: {worktreeItem?.Path ?? "null"}");
        
        if (string.IsNullOrEmpty(RepositoryPath))
        {
            var errorMsg = "No repository is currently open";
            ErrorMessage = errorMsg;
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }

        if (worktreeItem == null)
        {
            var errorMsg = "No worktree selected";
            ErrorMessage = errorMsg;
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }

        // Validate not main worktree
        if (worktreeItem.IsMainWorktree)
        {
            var errorMsg = "Cannot remove the main worktree";
            ErrorMessage = errorMsg;
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }

        if (!worktreeItem.CanRemove)
        {
            var errorMsg = "This worktree cannot be removed";
            ErrorMessage = errorMsg;
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }

        // Check if this is the currently open worktree
        if (IsCurrentWorktree(worktreeItem.Path))
        {
            var errorMsg = "Cannot remove the currently open worktree. Please close this solution first or switch to a different worktree.";
            ErrorMessage = errorMsg;
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }

        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;

        try
        {
            System.Diagnostics.Debug.WriteLine($"Calling GitService.RemoveWorktreeAsync for: {worktreeItem.Path}");
            
            var result = await _gitService.RemoveWorktreeAsync(
                RepositoryPath,
                worktreeItem.Path,
                force,
                cancellationToken);

            System.Diagnostics.Debug.WriteLine($"RemoveWorktreeAsync result: Success={result.Success}, Error={result.ErrorMessage}");

            if (result.Success)
            {
                SuccessMessage = $"Worktree '{worktreeItem.DisplayPath}' removed successfully!";
                // Refresh the list to reflect the removal
                await RefreshAsync(cancellationToken);
                return (true, null);
            }
            else
            {
                // Check for common error patterns and provide friendly messages
                var errorMessage = result.ErrorMessage ?? "Unknown error";
                var folderStillExists = System.IO.Directory.Exists(worktreeItem.Path);
                
                if (errorMessage.Contains("failed to delete") || errorMessage.Contains("Invalid argument"))
                {
                    if (folderStillExists)
                    {
                        // Git removed the reference but couldn't delete the folder
                        errorMessage = $"Git worktree reference removed, but the folder could not be deleted (files are in use).\n\nPlease manually delete: {worktreeItem.Path}";
                        
                        // Still refresh the list since the git reference is gone
                        await RefreshAsync(cancellationToken);
                    }
                    else
                    {
                        errorMessage = "Cannot remove worktree - files may be in use. Please close any open files or applications using this worktree.";
                    }
                }
                else if (errorMessage.Contains("modified or untracked files"))
                {
                    errorMessage = "Worktree has uncommitted changes. Commit or stash your changes first.";
                }
                
                ErrorMessage = errorMessage;
                
                // Show notification with details
                await ShowErrorNotificationAsync(
                    "Failed to remove worktree", 
                    errorMessage, 
                    cancellationToken);
                
                return (false, errorMessage);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error removing worktree: {ex.Message}";
            ErrorMessage = errorMsg;
            System.Diagnostics.Debug.WriteLine($"Exception in RemoveWorktreeAsync: {ex}");
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Adds a new worktree to the repository.
    /// </summary>
    /// <param name="worktreePath">The path where the new worktree will be created.</param>
    /// <param name="branchName">The branch name to checkout in the worktree.</param>
    /// <param name="createBranch">If true, creates a new branch.</param>
    /// <param name="baseBranch">The base branch to create the new branch from.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure with an error message.</returns>
    public async Task<(bool Success, string? ErrorMessage)> AddWorktreeAsync(
        string worktreePath,
        string branchName,
        bool createBranch = false,
        string? baseBranch = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(RepositoryPath))
        {
            var errorMsg = "No repository is currently open";
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }

        if (string.IsNullOrWhiteSpace(worktreePath))
        {
            var errorMsg = "Worktree path cannot be empty";
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }

        if (string.IsNullOrWhiteSpace(branchName))
        {
            var errorMsg = "Branch name cannot be empty";
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await _gitService.AddWorktreeAsync(
                RepositoryPath,
                worktreePath,
                branchName,
                createBranch,
                baseBranch,
                cancellationToken);

            if (result.Success)
            {
                // Refresh the list to show the new worktree
                await RefreshAsync(cancellationToken);
                return (true, null);
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
                
                // Show notification with git stderr details
                await ShowErrorNotificationAsync(
                    "Failed to add worktree", 
                    result.ErrorMessage, 
                    cancellationToken);
                
                return (false, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error adding worktree: {ex.Message}";
            ErrorMessage = errorMsg;
            await ShowErrorNotificationAsync(errorMsg, cancellationToken);
            return (false, errorMsg);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Opens a worktree folder in Windows Explorer.
    /// </summary>
    private async Task OpenInExplorerAsync(WorktreeItemViewModel worktreeItem, CancellationToken cancellationToken)
    {
        // Validate input
        if (worktreeItem == null)
        {
            ErrorMessage = "No worktree selected";
            return;
        }
        
        if (string.IsNullOrEmpty(worktreeItem.Path))
        {
            ErrorMessage = "Worktree path is empty";
            return;
        }

        var path = worktreeItem.Path;
        
        // Ensure the path exists
        if (!System.IO.Directory.Exists(path))
        {
            ErrorMessage = $"Directory does not exist: {path}";
            return;
        }

        try
        {
            // Use ProcessStartInfo with the folder path directly
            // Setting UseShellExecute = true and FileName = path opens the folder
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to open Explorer: {ex.Message}";
        }
    }

    /// <summary>
    /// Copies a worktree path to the clipboard.
    /// </summary>
    private Task CopyPathToClipboardAsync(WorktreeItemViewModel worktreeItem, CancellationToken cancellationToken)
    {
        if (worktreeItem == null || string.IsNullOrEmpty(worktreeItem.Path))
        {
            return Task.CompletedTask;
        }

        try
        {
            var path = worktreeItem.Path;
            
            // Use a thread with STA apartment state for clipboard access
            var thread = new System.Threading.Thread(() =>
            {
                System.Windows.Clipboard.SetDataObject(path, true);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();
            
            SuccessMessage = "Path copied to clipboard!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to copy path: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the filtered worktrees collection based on the search filter.
    /// </summary>
    private void UpdateFilteredWorktrees()
    {
        FilteredWorktrees.Clear();

        var filter = SearchFilter?.Trim() ?? string.Empty;
        
        foreach (var worktree in Worktrees)
        {
            if (string.IsNullOrEmpty(filter) ||
                worktree.DisplayPath.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                worktree.BranchName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                worktree.Path.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                FilteredWorktrees.Add(worktree);
            }
        }

        OnPropertyChanged(nameof(ShowNoWorktreesMessage));
        OnPropertyChanged(nameof(ShowWorktreeList));
    }

    /// <summary>
    /// Maps a Worktree model to a WorktreeItemViewModel.
    /// </summary>
    /// <param name="worktree">The worktree model to map.</param>
    /// <returns>The mapped view model.</returns>
    private WorktreeItemViewModel MapToViewModel(GitWorktreeManager.Models.Worktree worktree)
    {
        // Create a display-friendly path (just the folder name)
        var displayPath = System.IO.Path.GetFileName(worktree.Path.TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar));

        if (string.IsNullOrEmpty(displayPath))
        {
            displayPath = worktree.Path;
        }

        // Abbreviate the commit SHA for display (first 7 characters)
        var shortCommit = worktree.HeadCommit.Length > 7
            ? worktree.HeadCommit[..7]
            : worktree.HeadCommit;

        return new WorktreeItemViewModel
        {
            Path = worktree.Path,
            DisplayPath = displayPath,
            BranchName = worktree.Branch ?? "detached",
            HeadCommit = shortCommit,
            IsMainWorktree = worktree.IsMainWorktree,
            IsLocked = worktree.IsLocked,
            IsPrunable = worktree.IsPrunable,
            IsCurrentWorktree = IsCurrentWorktree(worktree.Path)
        };
    }

    /// <summary>
    /// Sets a property value and raises PropertyChanged if the value changed.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">The name of the property (auto-filled by compiler).</param>
    /// <returns>True if the value changed, false otherwise.</returns>
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
    /// <param name="propertyName">The name of the property that changed.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Shows an error notification to the user via the notification service.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task ShowErrorNotificationAsync(string message, CancellationToken cancellationToken = default)
    {
        if (_notificationService != null)
        {
            await _notificationService.ShowErrorAsync(message, null, cancellationToken);
        }
    }

    /// <summary>
    /// Shows an error notification with details (e.g., git stderr) to the user.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    /// <param name="details">Additional details such as git stderr output.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task ShowErrorNotificationAsync(string message, string? details, CancellationToken cancellationToken = default)
    {
        if (_notificationService != null)
        {
            await _notificationService.ShowErrorAsync(message, details, cancellationToken);
        }
    }

    /// <summary>
    /// Checks if the given path is the currently open worktree/solution.
    /// </summary>
    private bool IsCurrentWorktree(string worktreePath)
    {
        if (string.IsNullOrEmpty(RepositoryPath) || string.IsNullOrEmpty(worktreePath))
            return false;

        // Normalize paths for comparison
        var normalizedRepo = System.IO.Path.GetFullPath(RepositoryPath).TrimEnd(
            System.IO.Path.DirectorySeparatorChar, 
            System.IO.Path.AltDirectorySeparatorChar);
        var normalizedWorktree = System.IO.Path.GetFullPath(worktreePath).TrimEnd(
            System.IO.Path.DirectorySeparatorChar, 
            System.IO.Path.AltDirectorySeparatorChar);

        return string.Equals(normalizedRepo, normalizedWorktree, StringComparison.OrdinalIgnoreCase);
    }
}
