using Microsoft.VisualStudio.Extensibility.UI;
using GitWorktreeManager.ViewModels;

namespace GitWorktreeManager.ToolWindows;

/// <summary>
/// Remote UI control for the Worktree Manager tool window.
/// Uses XAML-based UI that runs in the VS process while logic runs out-of-process.
/// </summary>
internal class WorktreeToolWindowControl : RemoteUserControl
{
    /// <summary>
    /// Initializes a new instance of the WorktreeToolWindowControl.
    /// </summary>
    /// <param name="viewModel">The view model providing data and commands for the UI.</param>
    public WorktreeToolWindowControl(WorktreeViewModel viewModel)
        : base(viewModel)
    {
    }
}
