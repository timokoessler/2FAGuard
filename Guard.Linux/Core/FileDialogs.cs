using Avalonia.Platform.Storage;

namespace Guard.Linux.Core
{
    internal static class FileDialogs
    {
        /// <summary>
        /// Converts a Windows file dialog filter ("Name (*.a, *.b) | *.a; *.b | ...") into Avalonia file types.
        /// </summary>
        internal static List<FilePickerFileType> ParseFilter(string filter)
        {
            List<FilePickerFileType> types = [];
            string[] parts = filter.Split('|');
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                string[] patterns = parts[i + 1]
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                types.Add(new FilePickerFileType(parts[i].Trim()) { Patterns = patterns });
            }
            return types;
        }

        /// <summary>
        /// Shows an open file dialog and returns the local path of the selected file or null.
        /// </summary>
        internal static async Task<string?> OpenFile(string filter, string? title = null)
        {
            var files = await App.MainWindow!.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = false,
                    FileTypeFilter = ParseFilter(filter),
                }
            );
            return files.Count > 0 ? files[0].TryGetLocalPath() : null;
        }

        /// <summary>
        /// Shows a save file dialog and returns the local path of the selected file or null.
        /// </summary>
        internal static async Task<string?> SaveFile(string filter, string suggestedName, string? title = null)
        {
            var types = ParseFilter(filter);
            var file = await App.MainWindow!.StorageProvider.SaveFilePickerAsync(
                new FilePickerSaveOptions
                {
                    Title = title,
                    SuggestedFileName = suggestedName,
                    DefaultExtension = Path.GetExtension(suggestedName).TrimStart('.'),
                    FileTypeChoices = types,
                    ShowOverwritePrompt = true,
                }
            );
            return file?.TryGetLocalPath();
        }

        /// <summary>
        /// Shows a folder picker and returns the local path of the selected folder or null.
        /// </summary>
        internal static async Task<string?> PickFolder(string? title = null)
        {
            var folders = await App.MainWindow!.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = title, AllowMultiple = false }
            );
            return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        }
    }
}
