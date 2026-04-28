using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text;
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
        var content = await _fileService.PickAndReadTextAsync(FileTypes.Xml);
        if (content != null)
            RawXmlContent = content;
    }

    [RelayCommand]
    private async Task SelectXsltAsync()
    {
        var content = await _fileService.PickAndReadTextAsync(FileTypes.Xslt);
        if (content != null)
            XsltContent = content;
    }

    [RelayCommand]
    private async Task TransformAsync()
    {
        if (string.IsNullOrWhiteSpace(RawXmlContent) || string.IsNullOrWhiteSpace(XsltContent))
            return;

        IsBusy = true;
        try
        {
            // Offload CPU-heavy XSLT work to a background thread
            var transformedXml = await Task.Run(() => _xmlProcessorService.Transform(RawXmlContent, XsltContent));
            if (transformedXml == null)
            {
                _notificationService.HandleError("Empty transformation result", "XSLT Transformation Error");
                return;
            }

            // Update the UI properties
            TransformedResult = transformedXml;
            HasTransformedResult = true;
        }
        catch (Exception ex)
        {
            TransformedResult = string.Empty;
            _notificationService.HandleError(ex, "XSLT Transformation Error");
            HasTransformedResult = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(TransformedResult))
        {
            _notificationService.HandleError("Nothing to export. Run a transformation first.", "Export Error");
            return;
        }

        try
        {
            var result = await _fileService.SavePickAsync("transformed.xml", TransformedResult, ct);

            if (result.IsSuccessful)
            {
                await _notificationService.ShowSuccessAsync($"File saved: {result.FilePath}", ct);
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("Export cancelled.");
        }
        catch (Exception ex)
        {
            _notificationService.HandleError(ex, "Export Error");
        }
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
    private async Task GenericSumAsync(CancellationToken ct)
    {
        // Determine which string to parse
        string sourceContent = SelectedSource == "Raw XML" ? RawXmlContent : TransformedResult;

        if (string.IsNullOrWhiteSpace(sourceContent))
        {
            _notificationService.HandleError("Selected source is empty.", "Generic Sum Error");
            return;
        }

        IsBusy = true;
        try
        {
            // Offload CPU-heavy XML work to a background thread
            var modifiedXmlString = await Task.Run(() => _xmlProcessorService.ProcessXmlSum(
                sourceContent,
                TargetParentTag,
                TargetChildTag,
                TargetAttribute));

            if (modifiedXmlString == null)
            {
                _notificationService.HandleError($"No '{TargetParentTag}' tags found in the selected source.", "Generic Sum Error");
                return;
            }

            TransformedResult = modifiedXmlString;
            HasTransformedResult = true;
            await _notificationService.ShowSuccessAsync($"Generic sum was added to the {TargetParentTag} tag.", ct);
        }
        catch (Exception ex)
        {
            HasTransformedResult = false;
            _notificationService.HandleError(ex, "Generic Sum Error");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ScanTagsAsync()
    {
        string sourceContent = SelectedSource == "Raw XML" ? RawXmlContent : TransformedResult;
        if (string.IsNullOrWhiteSpace(sourceContent)) return;

        try
        {
            var allTags = await Task.Run(() => _xmlProcessorService.GetUniqueTags(sourceContent));
            string tagsFound = string.Join(", ", allTags);

            // TODO
            await Shell.Current.DisplayAlertAsync("Tags Found", tagsFound, "OK");
        }
        catch (Exception ex)
        {
            _notificationService.HandleError(ex, "Tags Error");
        }
    }
}