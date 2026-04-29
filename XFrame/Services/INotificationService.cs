namespace XFrame.Services;

/// <summary>
/// Notification Handler Service.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Processes and displays a system exception, typically including diagnostic details.
    /// </summary>
    /// <param name="ex">Exception being thrown.</param>
    /// <param name="title">Error title.</param>
    void HandleError(Exception ex, string? title = null);

    /// <summary>
    /// Displays a notification or validation message.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="title">Title.</param>
    void HandleAlert(string message, string? title = null);

    /// <summary>
    /// Displays a non-intrusive, brief message that dismisses automatically.
    /// </summary>
    /// <param name="message">Success message.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ShowToastAsync(string message, CancellationToken ct = default);
}