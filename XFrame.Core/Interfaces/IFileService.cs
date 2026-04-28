using XFrame.Core.Models;

namespace XFrame.Core.Interfaces
{
    /// <summary>
    /// Defines methods for file operations, including picking and reading files from the device, 
    /// and saving text content to files. It abstracts file handling functionality, allowing for 
    /// platform-specific implementations while providing a consistent API for the application.
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// For picking a file from the device
        /// </summary>
        /// <param name="filePickerFileType">File Picker with file type</param>
        Task<string?> PickAndReadTextAsync(object filePickerFileType);

        /// <summary>
        /// For reading sample files
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        Task<string> ReadAssetAsync(string fileName);

        /// <summary>
        /// For saving (optional, could also replace _fileSaver)
        /// </summary>
        /// <param name="fileName">The name of the file</param>
        /// <param name="content">The file content</param>
        Task SaveTextAsync(string fileName, string content);

        /// <summary>
        /// Returns a result object containing Success status and FilePath
        /// </summary>
        /// <param name="defaultFileName">The name of the file</param>
        /// <param name="content">The file content</param>
        /// <param name="ct">The cancellation token</param>
        Task<FileSaveResult> SavePickAsync(string defaultFileName, string content, CancellationToken ct);
    }
}
