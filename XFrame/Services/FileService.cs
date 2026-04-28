using CommunityToolkit.Maui.Storage;
using System.Text;
using XFrame.Core.Interfaces;
using XFrame.Core.Models;

namespace XFrame.Services
{
    /// <inheritdoc />
    public class FileService : IFileService
    {
        private readonly IFileSaver _fileSaver;
        public FileService(IFileSaver fileSaver) => _fileSaver = fileSaver;

        /// <inheritdoc />
        public async Task<string?> PickAndReadTextAsync(object filePickerFileType)
        {
            var options = new PickOptions
            {
                FileTypes = (FilePickerFileType)filePickerFileType
            };

            var result = await FilePicker.PickAsync(options);
            return result != null ? await File.ReadAllTextAsync(result.FullPath) : null;
        }

        /// <inheritdoc />
        public async Task<string> ReadAssetAsync(string fileName)
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        /// <inheritdoc />
        public async Task SaveTextAsync(string fileName, string content)
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, fileName);
            await File.WriteAllTextAsync(path, content);
        }

        /// <inheritdoc />
        public async Task<FileSaveResult> SavePickAsync(string defaultFileName, string content, CancellationToken ct)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

            var result = await _fileSaver.SaveAsync(defaultFileName, stream, ct);

            return new FileSaveResult(result.IsSuccessful, result.FilePath);
        }
    }
}
