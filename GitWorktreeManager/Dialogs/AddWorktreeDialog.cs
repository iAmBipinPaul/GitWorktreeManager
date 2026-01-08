using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Shell;
using GitWorktreeManager.ViewModels;

namespace GitWorktreeManager.Dialogs;

/// <summary>
/// Choice options for branch name prefix selection.
/// </summary>
public enum BranchPrefixChoice
{
    Feature,
    Bugfix,
    Release,
    Hotfix,
    Custom,
    Cancel
}

/// <summary>
/// Choice options for branch creation mode.
/// </summary>
public enum BranchCreationChoice
{
    CreateNew,
    CheckoutExisting,
    Cancel
}

/// <summary>
/// Choice options for worktree path selection.
/// </summary>
public enum WorktreePathChoice
{
    UseSuggested,
    UseParentFolder,
    Cancel
}

/// <summary>
/// Manages the Add Worktree dialog interaction.
/// Uses VS Shell prompts to collect user input for creating a new worktree.
/// </summary>
public class AddWorktreeDialog
{
    private readonly VisualStudioExtensibility _extensibility;

    /// <summary>
    /// Initializes a new instance of the AddWorktreeDialog.
    /// </summary>
    /// <param name="extensibility">The VS extensibility object for accessing shell services.</param>
    public AddWorktreeDialog(VisualStudioExtensibility extensibility)
    {
        _extensibility = extensibility ?? throw new ArgumentNullException(nameof(extensibility));
    }

    /// <summary>
    /// Shows the Add Worktree dialog using a series of VS prompts.
    /// </summary>
    /// <param name="repositoryPath">The repository path to suggest default worktree location.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The dialog result, or null if the user cancelled.</returns>
    public async Task<AddWorktreeDialogResult?> ShowAsync(
        string? repositoryPath = null,
        CancellationToken cancellationToken = default)
    {
        var shell = _extensibility.Shell();

        // Step 1: Get branch name prefix
        var prefixChoice = await PromptForBranchPrefixAsync(shell, cancellationToken);
        if (prefixChoice == BranchPrefixChoice.Cancel)
        {
            return null;
        }

        // Step 2: Get the branch suffix/name
        var branchName = await PromptForBranchNameAsync(shell, prefixChoice, cancellationToken);
        if (string.IsNullOrEmpty(branchName))
        {
            return null;
        }

        // Step 3: Ask if user wants to create a new branch
        var creationChoice = await PromptForCreateBranchAsync(shell, branchName, cancellationToken);
        if (creationChoice == BranchCreationChoice.Cancel)
        {
            return null;
        }

        // Step 4: Get worktree path
        var pathChoice = await PromptForWorktreePathAsync(shell, repositoryPath, branchName, cancellationToken);
        if (pathChoice == WorktreePathChoice.Cancel)
        {
            return null;
        }

        var worktreePath = GetWorktreePathFromChoice(pathChoice, repositoryPath, branchName);

        return new AddWorktreeDialogResult
        {
            BranchName = branchName,
            WorktreePath = worktreePath,
            CreateNewBranch = creationChoice == BranchCreationChoice.CreateNew
        };
    }

    /// <summary>
    /// Prompts the user to select a branch name prefix.
    /// </summary>
    private async Task<BranchPrefixChoice> PromptForBranchPrefixAsync(
        ShellExtensibility shell,
        CancellationToken cancellationToken)
    {
        return await shell.ShowPromptAsync(
            "Add Worktree - Select Branch Type\n\n" +
            "Choose a branch prefix or select Custom to enter a full branch name:",
            new PromptOptions<BranchPrefixChoice>
            {
                DismissedReturns = BranchPrefixChoice.Cancel,
                DefaultChoiceIndex = 0,
                Choices =
                {
                    { "feature/", BranchPrefixChoice.Feature },
                    { "bugfix/", BranchPrefixChoice.Bugfix },
                    { "release/", BranchPrefixChoice.Release },
                    { "hotfix/", BranchPrefixChoice.Hotfix },
                    { "Custom (enter full name)", BranchPrefixChoice.Custom },
                    { "Cancel", BranchPrefixChoice.Cancel }
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Prompts the user to enter the branch name suffix.
    /// </summary>
    private async Task<string?> PromptForBranchNameAsync(
        ShellExtensibility shell,
        BranchPrefixChoice prefixChoice,
        CancellationToken cancellationToken)
    {
        var prefix = prefixChoice switch
        {
            BranchPrefixChoice.Feature => "feature/",
            BranchPrefixChoice.Bugfix => "bugfix/",
            BranchPrefixChoice.Release => "release/",
            BranchPrefixChoice.Hotfix => "hotfix/",
            _ => string.Empty
        };

        var promptMessage = string.IsNullOrEmpty(prefix)
            ? "Add Worktree - Enter Branch Name\n\n" +
              "Enter the full branch name (e.g., 'main', 'develop', 'feature/my-feature'):"
            : $"Add Worktree - Complete Branch Name\n\n" +
              $"Branch prefix: {prefix}\n\n" +
              "Select a common suffix or choose Custom:";

        // For simplicity, provide common branch name options
        var suffixChoice = await shell.ShowPromptAsync(
            promptMessage,
            new PromptOptions<BranchSuffixChoice>
            {
                DismissedReturns = BranchSuffixChoice.Cancel,
                DefaultChoiceIndex = 0,
                Choices =
                {
                    { "new-feature", BranchSuffixChoice.NewFeature },
                    { "my-work", BranchSuffixChoice.MyWork },
                    { "experiment", BranchSuffixChoice.Experiment },
                    { "main", BranchSuffixChoice.Main },
                    { "develop", BranchSuffixChoice.Develop },
                    { "Cancel", BranchSuffixChoice.Cancel }
                }
            },
            cancellationToken);

        if (suffixChoice == BranchSuffixChoice.Cancel)
        {
            return null;
        }

        var suffix = suffixChoice switch
        {
            BranchSuffixChoice.NewFeature => "new-feature",
            BranchSuffixChoice.MyWork => "my-work",
            BranchSuffixChoice.Experiment => "experiment",
            BranchSuffixChoice.Main => "main",
            BranchSuffixChoice.Develop => "develop",
            _ => "new-work"
        };

        return prefix + suffix;
    }

    /// <summary>
    /// Prompts the user to choose whether to create a new branch.
    /// </summary>
    private async Task<BranchCreationChoice> PromptForCreateBranchAsync(
        ShellExtensibility shell,
        string branchName,
        CancellationToken cancellationToken)
    {
        return await shell.ShowPromptAsync(
            $"Add Worktree - Branch Creation\n\n" +
            $"Branch: {branchName}\n\n" +
            "Do you want to create a new branch, or checkout an existing branch?",
            new PromptOptions<BranchCreationChoice>
            {
                DismissedReturns = BranchCreationChoice.Cancel,
                DefaultChoiceIndex = 0,
                Choices =
                {
                    { "Create new branch (-b flag)", BranchCreationChoice.CreateNew },
                    { "Checkout existing branch", BranchCreationChoice.CheckoutExisting },
                    { "Cancel", BranchCreationChoice.Cancel }
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Prompts the user to select the worktree path.
    /// </summary>
    private async Task<WorktreePathChoice> PromptForWorktreePathAsync(
        ShellExtensibility shell,
        string? repositoryPath,
        string branchName,
        CancellationToken cancellationToken)
    {
        var suggestedPath = GetSuggestedWorktreePath(repositoryPath, branchName);
        var parentPath = GetParentFolderPath(repositoryPath, branchName);

        return await shell.ShowPromptAsync(
            $"Add Worktree - Select Location\n\n" +
            $"Branch: {branchName}\n\n" +
            "Choose where to create the worktree:\n\n" +
            $"Option 1: {suggestedPath}\n" +
            $"Option 2: {parentPath}",
            new PromptOptions<WorktreePathChoice>
            {
                DismissedReturns = WorktreePathChoice.Cancel,
                DefaultChoiceIndex = 0,
                Choices =
                {
                    { "Use suggested path (repo-branch)", WorktreePathChoice.UseSuggested },
                    { "Use parent folder path", WorktreePathChoice.UseParentFolder },
                    { "Cancel", WorktreePathChoice.Cancel }
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Gets the worktree path based on the user's choice.
    /// </summary>
    private static string GetWorktreePathFromChoice(
        WorktreePathChoice choice,
        string? repositoryPath,
        string branchName)
    {
        return choice switch
        {
            WorktreePathChoice.UseSuggested => GetSuggestedWorktreePath(repositoryPath, branchName),
            WorktreePathChoice.UseParentFolder => GetParentFolderPath(repositoryPath, branchName),
            _ => GetSuggestedWorktreePath(repositoryPath, branchName)
        };
    }

    /// <summary>
    /// Gets a suggested worktree path based on the repository path and branch name.
    /// </summary>
    private static string GetSuggestedWorktreePath(string? repositoryPath, string branchName)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "worktrees",
                SanitizeBranchName(branchName));
        }

        var repoParent = Path.GetDirectoryName(repositoryPath);
        if (string.IsNullOrEmpty(repoParent))
        {
            repoParent = repositoryPath;
        }

        var repoName = Path.GetFileName(
            repositoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        
        return Path.Combine(repoParent, $"{repoName}-{SanitizeBranchName(branchName)}");
    }

    /// <summary>
    /// Gets a path in the parent folder of the repository.
    /// </summary>
    private static string GetParentFolderPath(string? repositoryPath, string branchName)
    {
        if (string.IsNullOrEmpty(repositoryPath))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                SanitizeBranchName(branchName));
        }

        var repoParent = Path.GetDirectoryName(repositoryPath);
        if (string.IsNullOrEmpty(repoParent))
        {
            repoParent = repositoryPath;
        }

        return Path.Combine(repoParent, SanitizeBranchName(branchName));
    }

    /// <summary>
    /// Sanitizes a branch name for use in a file path.
    /// </summary>
    private static string SanitizeBranchName(string branchName)
    {
        var sanitized = branchName
            .Replace('/', '-')
            .Replace('\\', '-')
            .Replace(':', '-')
            .Replace('*', '-')
            .Replace('?', '-')
            .Replace('"', '-')
            .Replace('<', '-')
            .Replace('>', '-')
            .Replace('|', '-');

        return sanitized;
    }
}

/// <summary>
/// Choice options for branch name suffix selection.
/// </summary>
public enum BranchSuffixChoice
{
    NewFeature,
    MyWork,
    Experiment,
    Main,
    Develop,
    Cancel
}

/// <summary>
/// Result from the Add Worktree dialog.
/// </summary>
public class AddWorktreeDialogResult
{
    /// <summary>
    /// The branch name entered by the user.
    /// </summary>
    public required string BranchName { get; init; }

    /// <summary>
    /// The file system path where the worktree will be created.
    /// </summary>
    public required string WorktreePath { get; init; }

    /// <summary>
    /// If true, creates a new branch with the specified name.
    /// </summary>
    public bool CreateNewBranch { get; init; }
}
