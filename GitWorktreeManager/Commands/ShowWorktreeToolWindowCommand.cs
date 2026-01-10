using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using GitWorktreeManager.ToolWindows;

namespace GitWorktreeManager.Commands;

/// <summary>
/// Command to show the Worktree Manager tool window.
/// Placed in View > Other Windows menu and Extensions menu for easy access.
/// </summary>
[VisualStudioContribution]
public class ShowWorktreeToolWindowCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the ShowWorktreeToolWindowCommand.
    /// </summary>
    public ShowWorktreeToolWindowCommand()
    {
    }

    /// <summary>
    /// Gets the configuration for this command.
    /// </summary>
    public override CommandConfiguration CommandConfiguration =>
        new("%GitWorktreeManager.ShowWorktreeToolWindowCommand.DisplayName%")
        {
            // Place the command in View > Other Windows AND Extensions menu
            Placements =
            [
                CommandPlacement.KnownPlacements.ViewOtherWindowsMenu,
                CommandPlacement.KnownPlacements.ExtensionsMenu
            ],
            Icon = new CommandIconConfiguration(ImageMoniker.KnownValues.GitRepository, IconSettings.IconAndText)
        };

    /// <summary>
    /// Executes the command to show the Worktree Manager tool window.
    /// </summary>
    /// <param name="context">The command execution context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken) =>
        await Extensibility.Shell().ShowToolWindowAsync<WorktreeToolWindow>(true, cancellationToken);
}
