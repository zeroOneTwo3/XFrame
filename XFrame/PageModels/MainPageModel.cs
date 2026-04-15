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
    private static readonly FilePickerFileType xmlFileType = new FilePickerFileType(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.iOS, new[] { "public.xml" } },
            { DevicePlatform.Android, new[] { "application/xml", "text/xml" } },
            { DevicePlatform.WinUI, new[] { ".xml" } },
            { DevicePlatform.MacCatalyst, new[] { "public.xml" } },
        });

    private static readonly FilePickerFileType xsltFileType = new FilePickerFileType(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.iOS, new[] { "public.xml", "com.netscape.javascript-source" } }, // iOS can be picky with XSLT UIs
            { DevicePlatform.Android, new[] { "application/xml", "text/xml", "text/plain" } },
            { DevicePlatform.WinUI, new[] { ".xslt", ".xsl" } },
            { DevicePlatform.MacCatalyst, new[] { "public.xml" } },
        });

    private static readonly XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
    {
        Indent = true,
        OmitXmlDeclaration = false
    };

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
            FileTypes = xmlFileType
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
            FileTypes = xsltFileType
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
            var modifiedXmlString = await Task.Run(() => ProcessXmlSum(sourceContent));
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

    private string? ProcessXmlSum(string sourceContent)
    {
        var doc = XDocument.Parse(sourceContent);
        var targetNode = doc.Descendants(TargetParentTag).FirstOrDefault();
        if (targetNode == null)
        {
            return null;
        }

        var targetNodes = targetNode.Parent == null
            ? [targetNode]
            : targetNode.Parent.Elements();

        foreach (var parent in targetNodes)
        {
            //  Only add the 'total' attribute if we actually found children with the specified attribute
            if (parent.Elements(TargetChildTag).Any(e => e.Attribute(TargetAttribute) != null))
            {
                double total = parent.Elements(TargetChildTag)
                    .Select(child => ParseAmount((string?)child.Attribute(TargetAttribute)))
                    .Sum();

                parent.SetAttributeValue("total", total.ToString("F2", CultureInfo.InvariantCulture));
            }
        }

        using var stringWriter = new StringWriter();
        using (var writer = XmlWriter.Create(stringWriter, xmlWriterSettings))
        {
            doc.Save(writer);
        }

        return stringWriter.ToString();
    }

    private double ParseAmount(string? val)
    {
        var input = val?.Trim();
        if (string.IsNullOrWhiteSpace(input) || !char.IsDigit(input.First()))
            return 0;

        input = input.Replace(',', '.');
        // Try parsing with invariant culture first
        if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            return result;

        // If that fails, try the current culture (handles comma/dot issues)
        if (double.TryParse(input, NumberStyles.Any, CultureInfo.CurrentCulture, out result))
            return result;

        // If both fail, return 0 or throw an error
        return 0;
    }

    [RelayCommand]
    private async Task ScanTagsAsync()
    {
        string sourceContent = SelectedSource == "Raw XML" ? RawXmlContent : TransformedResult;
        if (string.IsNullOrWhiteSpace(sourceContent)) return;

        try
        {
            var allTags = new HashSet<string>();
            await Task.Run(() => {
                using var reader = XmlReader.Create(new StringReader(sourceContent));
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                        allTags.Add(reader.Name);
                }
            });

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