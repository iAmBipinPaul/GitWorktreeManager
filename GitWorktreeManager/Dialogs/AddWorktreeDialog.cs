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

        // Create completion source
        var resultTcs = new TaskCompletionSource<AddWorktreeDialogResult?>();

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
                    BaseBranch = dialogData.CreateNewBranch ? dialogData.SelectedBranch : null
                });
            }
            await Task.CompletedTask;
        });

        dialogData.CancelCommand = new AsyncCommand(async (_, ct) =>
        {
            resultTcs.TrySetResult(null);
            await Task.CompletedTask;
        });

        // Show dialog using VS Extensibility
        var dialogControl = new AddWorktreeDialogControl(dialogData);
        
        try
        {
            await _extensibility.Shell().ShowDialogAsync(
                dialogControl,
                "Add Worktree",
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        // Wait for result with timeout
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await resultTcs.Task.WaitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
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
}
