using Microsoft.VisualStudio.Extensibility.UI;

namespace GitWorktreeManager.Dialogs;

/// <summary>
/// Remote UI control for the Delete Confirmation dialog.
/// </summary>
internal class DeleteConfirmationDialogControl : RemoteUserControl
{
    public DeleteConfirmationDialogControl(DeleteConfirmationDialogData dataContext)
        : base(dataContext)
    {
    }
}
