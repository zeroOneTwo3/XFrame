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
    public async Task ShowSuccessAsync(string message, CancellationToken ct = default)
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
    public void HandleError(string error, string? title = null)
    {
        DisplayAlertAsync(error, title).FireAndForgetSafeAsync();
    }

    async Task DisplayAlertAsync(string error, string? title = null)
    {
        try
        {
            await _semaphore.WaitAsync();

            if (Shell.Current is Shell shell)
                await shell.DisplayAlertAsync(title ?? "Error", error, "OK");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}