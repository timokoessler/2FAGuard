using Guard.Core.Models;

namespace Guard.Core.Import.Importer
{
    public class ClipboardImporter : IImporter
    {
        public string Name => "Clipboard";
        public IImporter.ImportType Type => IImporter.ImportType.Clipboard;
        public string SupportedFileExtensions => "";

        public bool RequiresPassword(string? path) => false;

        public (int total, int duplicate, int tokenID) Parse(string? path, byte[]? password)
        {
            // Note: Clipboard access is platform-specific and should be handled by the UI layer
            // This importer expects the text to be passed via the path parameter
            // or retrieved via platform-specific clipboard service
            throw new NotSupportedException("Clipboard import requires platform-specific implementation. Use the UI layer to get clipboard content and pass to UriListImporter or QRFileImporter.");
        }
    }
}