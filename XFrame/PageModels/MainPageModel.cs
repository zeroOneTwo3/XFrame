using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Xml;
using System.Xml.Xsl;
using XFrame.Models;

namespace XFrame.PageModels;

public partial class MainPageModel : ObservableObject
{
    [ObservableProperty]
    private string rawXmlContent;

    [ObservableProperty]
    private string xsltContent;

    [ObservableProperty]
    private string transformedResult;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool hasTransformedResult;

    // Define the XML file type for all platforms
    private readonly FilePickerFileType xmlFileType = new FilePickerFileType(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.iOS, new[] { "public.xml" } },
            { DevicePlatform.Android, new[] { "application/xml", "text/xml" } },
            { DevicePlatform.WinUI, new[] { ".xml", ".xslt", ".xsl" } },
            { DevicePlatform.MacCatalyst, new[] { "public.xml" } },
        });

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
            OnPropertyChanged(nameof(HasTransformedResult));
        }
        catch (Exception ex)
        {
            TransformedResult = $"Error: {ex.Message}";
            OnPropertyChanged(nameof(HasTransformedResult));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        // Simple export using local file system for Desktop
        var path = Path.Combine(FileSystem.CacheDirectory, "transformed.xml");
        await File.WriteAllTextAsync(path, TransformedResult);

        await Shell.Current.DisplayAlert("Exported", $"File saved to temporary location: {path}", "OK");
        // Note: For a true 'Save As' dialog, use CommunityToolkit.Maui.Storage.FileSaver
    }
}