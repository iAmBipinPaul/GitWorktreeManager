using Microsoft.VisualStudio.Extensibility.UI;

namespace GitWorktreeManager.Dialogs;

/// <summary>
/// Remote UI control for the Add Worktree dialog.
/// Uses XAML-based UI that runs in the VS process while logic runs out-of-process.
/// </summary>
internal class AddWorktreeDialogControl : RemoteUserControl
{
    /// <summary>
    /// Initializes a new instance of the AddWorktreeDialogControl.
    /// </summary>
    /// <param name="dataContext">The data context providing data and commands for the dialog.</param>
    public AddWorktreeDialogControl(AddWorktreeDialogData dataContext)
        : base(dataContext: dataContext)
    {
    }
}
