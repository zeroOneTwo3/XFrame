using Moq;
using XFrame.Configuration;
using XFrame.Core.Interfaces;
using XFrame.PageModels;
using XFrame.Services;

public class MainPageModelTests
{
    private readonly Mock<INotificationService> _mockNotification;
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<IXmlProcessorService> _mockXmlService;
    private readonly MainPageModel _viewModel;

    public MainPageModelTests()
    {
        _mockNotification = new Mock<INotificationService>();
        _mockFileService = new Mock<IFileService>();
        _mockXmlService = new Mock<IXmlProcessorService>();

        // Inject the mocks into your ViewModel
        _viewModel = new MainPageModel(_mockNotification.Object, _mockXmlService.Object, _mockFileService.Object);
    }

    [Fact]
    public async Task SelectXmlCommand_WhenFileIsNull_ShouldNotUpdateContent()
    {
        // Arrange: Mock the file service to return null (user canceled picker)
        _mockFileService.Setup(s => s.PickAndReadTextAsync(FileTypes.Xml)).ReturnsAsync((string?)null);
        _viewModel.RawXmlContent = "example content";
        var originalContent = _viewModel.RawXmlContent;

        // Act
        await _viewModel.SelectXmlCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(originalContent, _viewModel.RawXmlContent);
        Assert.False(_viewModel.IsBusy);
        _mockNotification.Verify(n => n.HandleAlert(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockNotification.Verify(n => n.HandleError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SelectXsltCommand_WhenFileIsNull_ShouldNotUpdateContent()
    {
        // Arrange: Mock the file service to return null (user canceled picker)
        _mockFileService.Setup(s => s.PickAndReadTextAsync(FileTypes.Xslt)).ReturnsAsync((string?)null);
        _viewModel.XsltContent = "example content";
        var originalContent = _viewModel.XsltContent;

        // Act
        await _viewModel.SelectXsltCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(originalContent, _viewModel.XsltContent);
        Assert.False(_viewModel.IsBusy);
        _mockNotification.Verify(n => n.HandleAlert(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockNotification.Verify(n => n.HandleError(It.IsAny<Exception>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessXml_WhenExceptionOccurs_ShouldHandleError()
    {
        // Arrange: Force the file service to throw an exception
        var testException = new Exception("Disk Full");
        _mockFileService.Setup(s => s.PickAndReadTextAsync(FileTypes.Xml)).ThrowsAsync(testException);

        // Act
        await _viewModel.SelectXmlCommand.ExecuteAsync(null);

        // Assert: Verify that the Error handler was used, not just a simple alert
        _mockNotification.Verify(n => n.HandleError(testException, It.IsAny<string>()), Times.Once);
    }
}