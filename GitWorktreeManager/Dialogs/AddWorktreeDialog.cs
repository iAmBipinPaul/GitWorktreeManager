using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Shell;
using GitWorktreeManager.Services;
using Microsoft.VisualStudio.Extensibility.UI;

namespace GitWorktreeManager.Dialogs;

/// <summary>
/// Manages the Add Worktree dialog.
/// Shows a single dialog with branch name, base branch dropdown, and create option.
/// </summary>
public class AddWorktreeDialog
{
    private readonly VisualStudioExtensibility _extensibility;
    private readonly IGitService _gitService;

    public AddWorktreeDialog(VisualStudioExtensibility extensibility, IGitService? gitService = null)
    {
        _extensibility = extensibility ?? throw new ArgumentNullException(nameof(extensibility));
        _gitService = gitService ?? new GitService();
    }

    /// <summary>
    /// Shows the Add Worktree dialog.
    /// </summary>
    public async Task<AddWorktreeDialogResult?> ShowAsync(
        string? repositoryPath = null,
        CancellationToken cancellationToken = default)
    {
        // Load branches first
        var branches = await LoadBranchesAsync(repositoryPath, cancellationToken);
        
        if (branches.Count == 0)
        {
            // No branches found - show error
            await _extensibility.Shell().ShowPromptAsync(
                "No branches found in the repository.",
                new PromptOptions<bool>
                {
                    DismissedReturns = false,
                    Choices = { { "OK", true } }
                },
                cancellationToken);
            return null;
        }

        // Create dialog data
        var dialogData = new AddWorktreeDialogData
        {
            RepositoryPath = repositoryPath ?? string.Empty,
            CreateNewBranch = true
        };

        // Add branches
        foreach (var branch in branches)
        {
            dialogData.AvailableBranches.Add(branch);
        }

        // Set default selected branch (main, master, or first)
        dialogData.SelectedBranch = branches.FirstOrDefault(b => b == "main") 
            ?? branches.FirstOrDefault(b => b == "master") 
            ?? branches.FirstOrDefault();

        // Create completion source for result
        var resultTcs = new TaskCompletionSource<AddWorktreeDialogResult?>();
        
        // Create cancellation source to close the dialog
        var dialogCts = new CancellationTokenSource();

        // Set up commands
        dialogData.OkCommand = new AsyncCommand(async (_, ct) =>
        {
            if (dialogData.IsValid)
            {
                resultTcs.TrySetResult(new AddWorktreeDialogResult
                {
                    BranchName = dialogData.GetEffectiveBranchName(),
                    WorktreePath = dialogData.GetWorktreePath(),
                    CreateNewBranch = dialogData.CreateNewBranch,
                    BaseBranch = dialogData.CreateNewBranch ? dialogData.SelectedBranch : null,
                    OpenAfterCreation = dialogData.OpenAfterCreation
                });
                // Cancel to close the dialog
                await dialogCts.CancelAsync();
            }
        });

        dialogData.CancelCommand = new AsyncCommand(async (_, ct) =>
        {
            resultTcs.TrySetResult(null);
            // Cancel to close the dialog
            await dialogCts.CancelAsync();
        });

        // Show dialog using VS Extensibility
        var dialogControl = new AddWorktreeDialogControl(dialogData);
        
        // Link the dialog cancellation with the external cancellation token
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, dialogCts.Token);
        
        try
        {
            await _extensibility.Shell().ShowDialogAsync(
                dialogControl,
                "Add Worktree",
                linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Dialog was closed (either by user action or cancellation)
            // This is expected behavior
        }
        finally
        {
            dialogCts.Dispose();
        }

        // Return the result (will be null if cancelled)
        if (resultTcs.Task.IsCompleted)
        {
            return await resultTcs.Task;
        }
        
        return null;
    }

    private async Task<List<string>> LoadBranchesAsync(
        string? repositoryPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            return new List<string>();
        }

        try
        {
            var result = await _gitService.GetBranchesAsync(repositoryPath, cancellationToken);
            if (result.Success && result.Data != null)
            {
                return result.Data.ToList();
            }
        }
        catch
        {
            // Ignore errors
        }

        return new List<string>();
    }
}

/// <summary>
/// Result from the Add Worktree dialog.
/// </summary>
public class AddWorktreeDialogResult
{
    public required string BranchName { get; init; }
    public required string WorktreePath { get; init; }
    public bool CreateNewBranch { get; init; }
    public string? BaseBranch { get; init; }
    public bool OpenAfterCreation { get; init; }
}
