using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XFrame.Configuration;
using XFrame.Core.Interfaces;
using XFrame.Resources;

namespace XFrame.PageModels;

public partial class MainPageModel : ObservableObject
{
    [ObservableProperty]
    public partial string RawXmlContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string XsltContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TransformedResult { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool HasTransformedResult { get; set; }

    [ObservableProperty]
    public partial string TargetParentTag { get; set; }

    [ObservableProperty]
    public partial string TargetChildTag { get; set; }

    [ObservableProperty]
    public partial string TargetAttribute { get; set; }

    [ObservableProperty]
    public partial string SelectedSource { get; set; } = AppResources.SourceRawXml;

    private readonly INotificationService _notificationService;

    private readonly IXmlProcessorService _xmlProcessorService;

    private readonly IFileService _fileService;

    private readonly ISettingsService _settingsService;

    public MainPageModel(
        INotificationService notificationService,
        IXmlProcessorService xmlProcessorService,
        IFileService fileService,
        ISettingsService settingsService)
    {
        _notificationService = notificationService;
        _xmlProcessorService = xmlProcessorService;
        _fileService = fileService;
        _settingsService = settingsService;

        TargetParentTag = _settingsService.TargetParentTag;
        TargetChildTag = _settingsService.TargetChildTag;
        TargetAttribute = _settingsService.TargetAttribute;
    }

    [RelayCommand]
    private async Task SelectXmlAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            var content = await _fileService.PickAndReadTextAsync(FileTypes.Xml);
            if (content != null)
                RawXmlContent = content;
        }, AppResources.SelectXmlErrorTitle);
    }

    [RelayCommand]
    private async Task SelectXsltAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            var content = await _fileService.PickAndReadTextAsync(FileTypes.Xslt);
            if (content != null)
                XsltContent = content;
        }, AppResources.SelectXsltErrorTitle);
    }

    [RelayCommand]
    private async Task TransformAsync()
    {
        if (string.IsNullOrWhiteSpace(RawXmlContent) || string.IsNullOrWhiteSpace(XsltContent))
            return;

        TransformedResult = string.Empty;
        HasTransformedResult = false;

        await ExecuteBusyActionAsync(async () =>
        {
            // Offload CPU-heavy XSLT work to a background thread
            var transformedXml = await Task.Run(() => _xmlProcessorService.Transform(RawXmlContent, XsltContent));
            if (transformedXml == null)
            {
                _notificationService.HandleAlert(AppResources.EmptyTransformationMessage, AppResources.TransformationFailedTitle);
                return;
            }

            // Update the UI properties
            TransformedResult = transformedXml;
            HasTransformedResult = true;
        }, AppResources.TransformationFailedTitle);
    }

    [RelayCommand]
    private async Task ExportAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(TransformedResult))
        {
            _notificationService.HandleAlert(AppResources.ExportEmptyMessage, AppResources.ExportErrorTitle);
            return;
        }

        await ExecuteBusyActionAsync(async () =>
        {
            var result = await _fileService.SavePickAsync(AppConstants.DefaultExportFileName, TransformedResult, ct);

            if (result.IsSuccessful)
            {
                await _notificationService.ShowToastAsync(string.Format(AppResources.ExportSuccessMessage, result.FilePath), ct);
            }
        }, AppResources.ExportErrorTitle);
    }

    [RelayCommand]
    private async Task AppearingAsync()
    {
        // Only load samples if the editor is currently empty
        if (string.IsNullOrWhiteSpace(RawXmlContent))
        {
            await LoadSamplesAsync();
        }
    }

    public async Task LoadSamplesAsync()
    {
        try
        {
            RawXmlContent = await _fileService.ReadAssetAsync(AppConstants.Assets.SampleXml);
            XsltContent = await _fileService.ReadAssetAsync(AppConstants.Assets.TransformXslt);
        }
        catch (Exception ex)
        {
            _notificationService.HandleError(ex, AppResources.ErrorLoadSamples);
        }
    }

    [RelayCommand]
    private async Task GenericSumAsync(string? sourceContent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceContent))
        {
            _notificationService.HandleAlert(AppResources.EmptyContentMessage, AppResources.SumErrorTitle);
            return;
        }

        TransformedResult = string.Empty;
        HasTransformedResult = false;

        await ExecuteBusyActionAsync(async () =>
        {
            // Offload CPU-heavy XML work to a background thread
            var modifiedXmlString = await Task.Run(() => _xmlProcessorService.ProcessXmlSum(
                sourceContent,
                TargetParentTag,
                TargetChildTag,
                TargetAttribute));

            if (modifiedXmlString == null)
            {
                _notificationService.HandleAlert(
                    string.Format(AppResources.NoTagsFoundMessage, TargetParentTag),
                    AppResources.SumErrorTitle);
                return;
            }

            TransformedResult = modifiedXmlString;
            HasTransformedResult = true;
            await _notificationService.ShowToastAsync(string.Format(AppResources.SumResultMessage, TargetParentTag), ct);

        }, AppResources.SumErrorTitle);
    }

    [RelayCommand]
    private async Task ScanTagsAsync(string? sourceContent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceContent))
            return;

        try
        {
            var tagsFound = await Task.Run(() => string.Join(", ", _xmlProcessorService.GetUniqueTags(sourceContent, ct)));

            _notificationService.HandleAlert(tagsFound, AppResources.TagsFoundTitle);
        }
        catch (Exception ex)
        {
            _notificationService.HandleError(ex, AppResources.TagsErrorTitle);
        }
    }

    /// <summary>
    /// Runs actions with state management and error handling.
    /// </summary>
    /// <remarks>
    /// This method implements several critical patterns:
    /// <list type="bullet">
    /// <item>
    /// <description><b>Re-entrancy Guard:</b> Uses <see cref="IsBusy"/> as a semaphore to prevent multiple 
    /// simultaneous executions of the same action (e.g., rapid button double-tapping).</description>
    /// </item>
    /// <item>
    /// <description><b>Standardized Error Handling:</b> Automatically routes unhandled exceptions 
    /// to the <c>_notificationService</c> and logs them to debug output.</description>
    /// </item>
    /// <item>
    /// <description><b>Graceful Cancellation:</b> Specifically catches <see cref="OperationCanceledException"/> 
    /// to allow for silent failures during user-cancelled tasks like file picking.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="action">The asynchronous task or logic to be executed.</param>
    /// <param name="errorCaption">The localized title or context-specific header to show in the error dialog if the action fails.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the orchestrated action.</returns>
    private async Task ExecuteBusyActionAsync(Func<Task> action, string errorCaption = "Error")
    {
        if (IsBusy) // acts as a "semaphore," ensuring only one execution happens at a time
            return;

        IsBusy = true;

        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            // User cancelled (e.g., closed a file picker) - usually silent
            System.Diagnostics.Debug.WriteLine("Action was cancelled.");
        }
        catch (Exception ex)
        {
            // Centralized error handling
            _notificationService.HandleError(ex, errorCaption);

            // Log to telemetry/debug if needed
            System.Diagnostics.Debug.WriteLine($"[BusyAction Error]: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}