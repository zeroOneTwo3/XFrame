using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XFrame.Configuration;
using XFrame.Core.Interfaces;

namespace XFrame.PageModels;

public partial class MainPageModel : ObservableObject
{
    [ObservableProperty]
    private string rawXmlContent = string.Empty;

    [ObservableProperty]
    private string xsltContent = string.Empty;

    [ObservableProperty]
    private string transformedResult = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasTransformedResult;

    [ObservableProperty]
    private string targetParentTag = "Employee";

    [ObservableProperty]
    private string targetChildTag = "salary";

    [ObservableProperty]
    private string targetAttribute = "amount";

    [ObservableProperty]
    private string selectedSource = "Raw XML";

    private readonly INotificationService _notificationService;

    private readonly IXmlProcessorService _xmlProcessorService;

    private readonly IFileService _fileService;

    public MainPageModel(
        INotificationService notificationService,
        IXmlProcessorService xmlProcessorService,
        IFileService fileService)
    {
        _notificationService = notificationService;
        _xmlProcessorService = xmlProcessorService;
        _fileService = fileService;
    }

    [RelayCommand]
    private async Task SelectXmlAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            var content = await _fileService.PickAndReadTextAsync(FileTypes.Xml);
            if (content != null)
                RawXmlContent = content;
        }, "Failed to load XML file.");
    }

    [RelayCommand]
    private async Task SelectXsltAsync()
    {
        await ExecuteBusyActionAsync(async () =>
        {
            var content = await _fileService.PickAndReadTextAsync(FileTypes.Xslt);
            if (content != null)
                XsltContent = content;
        }, "Failed to load XML file.");
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
                _notificationService.HandleAlert("Empty transformation result", "XSLT Transformation Error");
                return;
            }

            // Update the UI properties
            TransformedResult = transformedXml;
            HasTransformedResult = true;
        }, "Transformation Failed");
    }

    [RelayCommand]
    private async Task ExportAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(TransformedResult))
        {
            _notificationService.HandleAlert("Nothing to export. Run a transformation first.", "Export Error");
            return;
        }

        await ExecuteBusyActionAsync(async () =>
        {
            var result = await _fileService.SavePickAsync("transformed.xml", TransformedResult, ct);

            if (result.IsSuccessful)
            {
                await _notificationService.ShowToastAsync($"File saved: {result.FilePath}", ct);
            }
        }, "Export Error");
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
            RawXmlContent = await _fileService.ReadAssetAsync("sample.xml");
            XsltContent = await _fileService.ReadAssetAsync("transform.xslt");
        }
        catch (Exception ex)
        {
            _notificationService.HandleError(ex, "Load Samples Error");
        }
    }

    [RelayCommand]
    private async Task GenericSumAsync(string? sourceContent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceContent))
        {
            _notificationService.HandleAlert("Selected source is empty.", "Generic Sum Error");
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
                _notificationService.HandleAlert($"No '{TargetParentTag}' tags found in the selected source.", "Generic Sum Error");
                return;
            }

            TransformedResult = modifiedXmlString;
            HasTransformedResult = true;
            await _notificationService.ShowToastAsync($"Generic sum was added to the {TargetParentTag} tag.", ct);

        }, "Generic Sum Error");
    }

    [RelayCommand]
    private async Task ScanTagsAsync(string? sourceContent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceContent))
            return;

        try
        {
            var allTags = await Task.Run(() => _xmlProcessorService.GetUniqueTags(sourceContent, ct));
            string tagsFound = string.Join(", ", allTags);

            _notificationService.HandleAlert("Tags Found", tagsFound);
        }
        catch (Exception ex)
        {
            _notificationService.HandleError(ex, "Tags Error");
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