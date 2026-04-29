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
        /// Launches a platform-specific file picker to read the contents of a text file.
        /// </summary>
        /// <param name="filePickerFileType">The platform-specific file type filter.</param>
        /// <returns>The text content of the file, or null if the operation was cancelled.</returns>
        Task<string?> PickAndReadTextAsync(object filePickerFileType);

        /// <summary>
        /// Reads the content of a file bundled within the application package (assets).
        /// </summary>
        /// <param name="fileName">The name/path of the asset file.</param>
        /// <returns>The text content of the asset.</returns>
        Task<string> ReadAssetAsync(string fileName);

        /// <summary>
        /// Persists text content to a file at a platform-specific location.
        /// </summary>
        /// <param name="fileName">The desired name of the file, including extension.</param>
        /// <param name="content">The raw string data to be written to the file using UTF-8 encoding.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous save operation.</returns>
        Task SaveTextAsync(string fileName, string content);

        /// <summary>
        /// Launches a "Save As" dialog and streams the provided content to the selected destination.
        /// </summary>
        /// <param name="defaultFileName">The suggested filename for the export.</param>
        /// <param name="content">The string content to be saved.</param>
        /// <param name="ct">Cancellation token to abort the save operation.</param>
        /// <returns>A <see cref="FileSaveResult"/> containing the success status and the final file path.</returns>
        Task<FileSaveResult> SavePickAsync(string defaultFileName, string content, CancellationToken ct);
    }
}
