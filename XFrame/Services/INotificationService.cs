namespace XFrame.Services;

/// <summary>
/// Error Handler Service.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Handle error in UI.
    /// </summary>
    /// <param name="ex">Exception being thrown.</param>
    /// <param name="title">Error title.</param>
    void HandleError(Exception ex, string? title = null);

    /// <summary>
    /// Handle error in UI.
    /// </summary>
    /// <param name="error">Error message.</param>
    /// <param name="title">Error title.</param>
    void HandleError(string error, string? title = null);

    /// <summary>
    /// Show success message in UI.
    /// </summary>
    /// <param name="message">Success message.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ShowSuccessAsync(string message, CancellationToken ct = default);
}