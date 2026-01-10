namespace GitWorktreeManager.Services;

/// <summary>
/// Severity levels for notifications.
/// </summary>
public enum NotificationSeverity
{
    /// <summary>
    /// Informational message.
    /// </summary>
    Information,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    Error
}

/// <summary>
/// Represents an action button for a notification.
/// </summary>
public class NotificationAction
{
    /// <summary>
    /// Gets the display text for the action button.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the callback to invoke when the action is clicked.
    /// </summary>
    public Func<CancellationToken, Task> Callback { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationAction"/> class.
    /// </summary>
    /// <param name="text">The display text for the action button.</param>
    /// <param name="callback">The callback to invoke when clicked.</param>
    public NotificationAction(string text, Func<CancellationToken, Task> callback)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }
}

/// <summary>
/// Service interface for displaying notifications to the user.
/// Supports error, warning, and info severity levels with optional action buttons.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a notification message to the user.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="severity">The severity level of the notification.</param>
    /// <param name="actions">Optional action buttons to include.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public Task ShowNotificationAsync(
        string message,
        NotificationSeverity severity = NotificationSeverity.Information,
        IEnumerable<NotificationAction>? actions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows an error notification to the user.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    /// <param name="details">Optional additional details (e.g., git stderr output).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public Task ShowErrorAsync(
        string message,
        string? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a warning notification to the user.
    /// </summary>
    /// <param name="message">The warning message to display.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public Task ShowWarningAsync(
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows an informational notification to the user.
    /// </summary>
    /// <param name="message">The informational message to display.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public Task ShowInfoAsync(
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows an error notification with an action button.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    /// <param name="actionText">The text for the action button.</param>
    /// <param name="actionCallback">The callback to invoke when the action is clicked.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public Task ShowErrorWithActionAsync(
        string message,
        string actionText,
        Func<CancellationToken, Task> actionCallback,
        CancellationToken cancellationToken = default);
}
