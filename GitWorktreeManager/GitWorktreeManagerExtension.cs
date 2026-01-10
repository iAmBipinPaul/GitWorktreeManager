using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using GitWorktreeManager.Services;

namespace GitWorktreeManager;

/// <summary>
/// Extension entry point for the Git Worktree Manager extension.
/// Uses the VisualStudio.Extensibility out-of-process model for improved reliability.
/// </summary>
[VisualStudioContribution]
public class GitWorktreeManagerExtension : Extension
{
    private readonly ILoggerService _logger;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the GitWorktreeManagerExtension.
    /// Logs extension initialization for troubleshooting.
    /// </summary>
    public GitWorktreeManagerExtension()
    {
        _logger = new ActivityLogService();
        _logger.LogInformation("Git Worktree Manager extension is initializing");
    }

    /// <inheritdoc />
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        Metadata = new ExtensionMetadata(
            "IAmBipinPaul.GitWorktreeManager.e7f8a9b0-c1d2-4e3f-5a6b-7c8d9e0f1a2b",
            ExtensionAssemblyVersion,
            "Bipin Paul",
            "Git Worktree Manager",
            "Manage Git worktrees from within Visual Studio. List, add, remove, and open Git worktrees directly from the IDE without leaving Visual Studio.")
    };

    /// <summary>
    /// Initializes services for the extension using dependency injection.
    /// </summary>
    /// <param name="serviceCollection">The service collection to configure.</param>
    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);

        _logger.LogInformation("Registering extension services");

        // Register logging service as singleton
        serviceCollection.AddSingleton<ILoggerService>(_logger);

        // Register Git service with logger dependency
        serviceCollection.AddSingleton<IGitService>(sp =>
        {
            ILoggerService? logger = sp.GetService<ILoggerService>();
            return new GitService(logger);
        });

        _logger.LogInformation(
            $"Git Worktree Manager extension initialized successfully (version: {ExtensionAssemblyVersion})");
    }

    /// <summary>
    /// Called when the extension is disposed.
    /// Logs extension disposal for troubleshooting.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            _logger.LogInformation("Git Worktree Manager extension is being disposed");
            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
