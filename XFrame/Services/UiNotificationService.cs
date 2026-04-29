using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace XFrame.Services;

/// <summary>
/// Modal Error Handler.
/// </summary>
public class UiNotificationService : INotificationService
{
    SemaphoreSlim _semaphore = new(1, 1);

    /// <inheritdoc />
    public async Task ShowToastAsync(string message, CancellationToken ct = default)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var toast = Toast.Make(message, ToastDuration.Short, 14);
            await toast.Show(ct);
        });
    }

    /// <inheritdoc />
    public void HandleError(Exception ex, string? title = null)
    {
        DisplayAlertAsync(ex.Message, title).FireAndForgetSafeAsync();
    }

    /// <inheritdoc />
    public void HandleAlert(string message, string? title = null)
    {
        DisplayAlertAsync(message, title).FireAndForgetSafeAsync();
    }

    /// <summary>
    /// Displays a modal alert dialog to the user.
    /// </summary>
    /// <param name="message">The content message to display.</param>
    /// <param name="title">Optional header title for the alert.</param>
    async Task DisplayAlertAsync(string message, string? title = null)
    {
        try
        {
            await _semaphore.WaitAsync();

            if (Shell.Current is Shell shell)
                await shell.DisplayAlertAsync(title ?? "Notification", message, "OK");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}