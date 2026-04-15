using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

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

    // Define the XML file type for all platforms
    private readonly FilePickerFileType xmlFileType = new FilePickerFileType(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.iOS, new[] { "public.xml" } },
            { DevicePlatform.Android, new[] { "application/xml", "text/xml" } },
            { DevicePlatform.WinUI, new[] { ".xml", ".xslt", ".xsl" } },
            { DevicePlatform.MacCatalyst, new[] { "public.xml" } },
        });

    private readonly IFileSaver _fileSaver;

    private readonly INotificationService _notificationService;

    public MainPageModel(IFileSaver fileSaver, INotificationService notificationService)
    {
        _fileSaver = fileSaver;
        _notificationService = notificationService;
    }

    [RelayCommand]
    private async Task SelectXmlAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select XML",
            FileTypes = xmlFileType // Use the custom type here
        });

        if (result != null)
            RawXmlContent = await File.ReadAllTextAsync(result.FullPath);
    }

    [RelayCommand]
    private async Task SelectXsltAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select XML",
            FileTypes = xmlFileType // Use the custom type here
        });

        if (result != null)
            XsltContent = await File.ReadAllTextAsync(result.FullPath);
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
            await Task.Run(() =>
            {
                using var xmlReader = XmlReader.Create(new StringReader(RawXmlContent));
                using var xsltReader = XmlReader.Create(new StringReader(XsltContent));

                var transformer = new XslCompiledTransform();
                transformer.Load(xsltReader);

                using var resultsWriter = new StringWriter();
                transformer.Transform(xmlReader, null, resultsWriter);

                // Update the UI properties
                TransformedResult = resultsWriter.ToString();
                HasTransformedResult = true;
            });
        }
        catch (Exception ex)
        {
            TransformedResult = string.Empty;
            _notificationService.HandleError(ex, "Transformation Error");
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
            // Convert the string to a stream for the FileSaver
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(TransformedResult));

            // This opens the native "Save As" dialog
            var fileSaverResult = await _fileSaver.SaveAsync("transformed.xml", stream, ct);

            if (fileSaverResult.IsSuccessful)
            {
                await _notificationService.ShowSuccessAsync($"File saved: {fileSaverResult.FilePath}", ct);
            }
            else
            {
                // This triggers if the user cancels the dialog
                System.Diagnostics.Debug.WriteLine("Export cancelled by user.");
            }
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
            // OpenAppPackageFileAsync reads directly from Resources/Raw
            using var xmlStream = await FileSystem.OpenAppPackageFileAsync("sample.xml");
            using var xmlReader = new StreamReader(xmlStream);
            RawXmlContent = await xmlReader.ReadToEndAsync();

            using var xsltStream = await FileSystem.OpenAppPackageFileAsync("transform.xslt");
            using var xsltReader = new StreamReader(xsltStream);
            XsltContent = await xsltReader.ReadToEndAsync();
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
            var doc = XDocument.Parse(sourceContent);
            var parents = doc.Descendants(TargetParentTag);

            foreach (var parent in parents)
            {
                double total = parent.Elements(TargetChildTag)
                    .Select(child =>
                    {
                        string attrVal = (string?)child.Attribute(TargetAttribute) ?? "0";
                        attrVal = attrVal.Replace(',', '.');
                        return double.TryParse(attrVal, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ? d : 0;
                    })
                    .Sum();

                parent.SetAttributeValue("total", total.ToString("F2", CultureInfo.InvariantCulture));
            }

            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false
            };

            using var stringWriter = new StringWriter();
            using (var writer = XmlWriter.Create(stringWriter, settings))
            {
                doc.Save(writer);
            }

            TransformedResult = stringWriter.ToString();
            HasTransformedResult = true;
            await _notificationService.ShowSuccessAsync($"Generic sum was added for {TargetParentTag} tag.", ct);
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
            var doc = XDocument.Parse(sourceContent);
            var allTags = doc.Descendants().Select(x => x.Name.LocalName).Distinct().ToList();
            string tagsFound = string.Join(", ", allTags);

            await Shell.Current.DisplayAlertAsync("Tags Found", tagsFound, "OK"); //TODO
        }
        catch(Exception ex)
        {
            _notificationService.HandleError(ex, "Tags Error");
        }
    }
}