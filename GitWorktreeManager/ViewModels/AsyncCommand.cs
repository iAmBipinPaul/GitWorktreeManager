using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.UI;

namespace GitWorktreeManager.ViewModels;

/// <summary>
/// Implementation of IAsyncCommand for use with Remote UI data binding.
/// Provides async command execution with parameter support.
/// </summary>
public class AsyncCommand : IAsyncCommand
{
    private readonly Func<object?, CancellationToken, Task> _execute;
    private readonly Func<object?, bool>? _canExecuteFunc;

    /// <summary>
    /// Initializes a new instance of the AsyncCommand.
    /// </summary>
    /// <param name="execute">The async function to execute.</param>
    /// <param name="canExecute">Optional function to determine if the command can execute.</param>
    public AsyncCommand(
        Func<object?, CancellationToken, Task> execute,
        Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecuteFunc = canExecute;
    }

    /// <summary>
    /// Gets whether the command can execute.
    /// </summary>
    public bool CanExecute => _canExecuteFunc?.Invoke(null) ?? true;

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <param name="parameter">The command parameter.</param>
    /// <param name="clientContext">The client context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task
        ExecuteAsync(object? parameter, IClientContext clientContext, CancellationToken cancellationToken) =>
        await _execute(parameter, cancellationToken);
}
