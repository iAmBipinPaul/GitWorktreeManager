namespace GitWorktreeManager.Services;

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Shell;

/// <summary>
/// Service for displaying notifications to the user via Visual Studio's InfoBar and prompts.
/// Supports error, warning, and info severity levels with optional action buttons.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly VisualStudioExtensibility _extensibility;
    private readonly ILoggerService? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    /// <param name="extensibility">The VS extensibility object for accessing shell services.</param>
    /// <param name="logger">Optional logger service for recording operations.</param>
    public NotificationService(VisualStudioExtensibility extensibility, ILoggerService? logger = null)
    {
        _extensibility = extensibility ?? throw new ArgumentNullException(nameof(extensibility));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ShowNotificationAsync(
        string message,
        NotificationSeverity severity = NotificationSeverity.Information,
        IEnumerable<NotificationAction>? actions = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        LogNotification(message, severity);

        try
        {
            ShellExtensibility shell = _extensibility.Shell();
            string title = GetNotificationTitle(severity);
            string fullMessage = $"{title}: {message}";

            if (actions != null && actions.Any())
            {
                // Show prompt with action buttons using enum-based options
                var actionList = actions.ToList();

                // For simplicity, we'll show a prompt with OK and let the first action be triggered
                // VS Extensibility SDK uses enum-based PromptOptions
                bool result = await shell.ShowPromptAsync(
                    fullMessage + "\n\nClick OK to proceed or Cancel to dismiss.",
                    PromptOptions.OKCancel,
                    cancellationToken);

                // If user clicked OK and there's an action, execute the first one
                if (result && actionList.Count > 0)
                {
                    try
                    {
                        await actionList[0].Callback(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogException(ex, $"Error executing notification action: {actionList[0].Text}");
                    }
                }
            }
            else
            {
                // Show simple prompt without actions
                await shell.ShowPromptAsync(fullMessage, PromptOptions.OK, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogException(ex, "Failed to show notification");
        }
    }

    /// <inheritdoc />
    public async Task ShowErrorAsync(
        string message,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        string fullMessage = string.IsNullOrEmpty(details)
            ? message
            : $"{message}\n\nDetails: {details}";

        await ShowNotificationAsync(fullMessage, NotificationSeverity.Error, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ShowWarningAsync(
        string message,
        CancellationToken cancellationToken = default) =>
        await ShowNotificationAsync(message, NotificationSeverity.Warning, null, cancellationToken);

    /// <inheritdoc />
    public async Task ShowInfoAsync(
        string message,
        CancellationToken cancellationToken = default) =>
        await ShowNotificationAsync(message, NotificationSeverity.Information, null, cancellationToken);

    /// <inheritdoc />
    public async Task ShowErrorWithActionAsync(
        string message,
        string actionText,
        Func<CancellationToken, Task> actionCallback,
        CancellationToken cancellationToken = default)
    {
        NotificationAction[] actions = new[] { new NotificationAction(actionText, actionCallback) };
        await ShowNotificationAsync(message, NotificationSeverity.Error, actions, cancellationToken);
    }

    /// <summary>
    /// Gets the notification title based on severity.
    /// </summary>
    private static string GetNotificationTitle(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Error => "Git Worktree Manager Error",
            NotificationSeverity.Warning => "Git Worktree Manager Warning",
            NotificationSeverity.Information => "Git Worktree Manager",
            _ => "Git Worktree Manager"
        };
    }

    /// <summary>
    /// Logs the notification at the appropriate level.
    /// </summary>
    private void LogNotification(string message, NotificationSeverity severity)
    {
        switch (severity)
        {
            case NotificationSeverity.Error:
                _logger?.LogError($"Notification (Error): {message}");
                break;
            case NotificationSeverity.Warning:
                _logger?.LogWarning($"Notification (Warning): {message}");
                break;
            case NotificationSeverity.Information:
            default:
                _logger?.LogInformation($"Notification (Info): {message}");
                break;
        }
    }
}
