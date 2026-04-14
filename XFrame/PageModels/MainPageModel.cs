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

    public MainPageModel(IFileSaver fileSaver)
    {
        _fileSaver = fileSaver;
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
    private void Transform()
    {
        if (string.IsNullOrWhiteSpace(RawXmlContent) || string.IsNullOrWhiteSpace(XsltContent))
            return;

        IsBusy = true;
        try
        {
            using var xmlReader = XmlReader.Create(new StringReader(RawXmlContent));
            using var xsltReader = XmlReader.Create(new StringReader(XsltContent));

            var transformer = new XslCompiledTransform();
            transformer.Load(xsltReader);

            using var resultsWriter = new StringWriter();
            transformer.Transform(xmlReader, null, resultsWriter);

            TransformedResult = resultsWriter.ToString();
            HasTransformedResult = true;
        }
        catch (Exception ex)
        {
            TransformedResult = $"Error: {ex.Message}";
            HasTransformedResult = false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(TransformedResult))
        {
            await Shell.Current.DisplayAlertAsync("Empty", "Nothing to export. Run a transformation first.", "OK");
            return;
        }

        try
        {
            // Convert the string to a stream for the FileSaver
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(TransformedResult));

            // This opens the native "Save As" dialog
            var fileSaverResult = await _fileSaver.SaveAsync("transformed.xml", stream, cancellationToken);

            if (fileSaverResult.IsSuccessful)
            {
                await Shell.Current.DisplayAlertAsync("Success", $"File saved: {fileSaverResult.FilePath}", "OK");
            }
            else
            {
                // This triggers if the user cancels the dialog
                System.Diagnostics.Debug.WriteLine("Export cancelled by user.");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Export Error", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task Appearing()
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
            await Shell.Current.DisplayAlertAsync("Error", $"Could not load sample files. {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task GenericSum()
    {
        // Determine which string to parse
        string sourceContent = SelectedSource == "Raw XML" ? RawXmlContent : TransformedResult;

        if (string.IsNullOrWhiteSpace(sourceContent))
        {
            await Shell.Current.DisplayAlertAsync("Error", "Selected source is empty.", "OK");
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

            // We always output to the Result pane
            TransformedResult = doc.ToString();
            HasTransformedResult = true;
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Generic Sum Error", ex.Message, "OK");
        }
        finally
        { 
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ScanTags()
    {
        string sourceContent = SelectedSource == "Raw XML" ? RawXmlContent : TransformedResult;
        if (string.IsNullOrWhiteSpace(sourceContent)) return;

        try
        {
            var doc = XDocument.Parse(sourceContent);
            var allTags = doc.Descendants().Select(x => x.Name.LocalName).Distinct().ToList();

            // Quick debug output or you could bind this to a dropdown
            string tagsFound = string.Join(", ", allTags);
            await Shell.Current.DisplayAlertAsync("Tags Found", tagsFound, "OK");
        }
        catch(Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}