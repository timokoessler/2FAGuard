using Guard.Core.Models;

namespace Guard.Core.Import.Importer
{
    public class QRScreenCaptureImporter : IImporter
    {
        public string Name => "QRScreenCapture";
        public IImporter.ImportType Type => IImporter.ImportType.ScreenCapture;
        public string SupportedFileExtensions => "";

        public bool RequiresPassword(string? path) => false;

        public (int total, int duplicate, int tokenID) Parse(string? path, byte[]? password)
        {
            // Screen capture is platform-specific and should be handled by the UI layer
            throw new NotSupportedException("Screen capture import requires platform-specific implementation. Use the UI layer to capture screen and pass image to QRFileImporter.");
        }
    }
}