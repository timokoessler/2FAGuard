using Guard.Linux.Core.Import;

namespace Guard.WPF.Core.Import.Importer
{
    internal class QRScreenCaptureImporter : IImporter
    {
        public string Name => "QRScreenCapture";
        public IImporter.ImportType Type => IImporter.ImportType.ScreenCapture;
        public string SupportedFileExtensions => "";

        public bool RequiresPassword(string? path) => false;

        private byte[]? screenshot;

        /// <summary>
        /// Takes the screenshot. Must be called before Parse.
        /// </summary>
        internal async Task Capture()
        {
            screenshot = await ScreenCapture.CaptureAllScreens();
        }

        public (int total, int duplicate, int tokenID) Parse(string? path, byte[]? password)
        {
            if (screenshot == null)
            {
                throw new InvalidOperationException("No screenshot taken");
            }
            string text =
                QRCode.ParseQRImage(screenshot)
                ?? throw new Exception(I18n.GetString("import.noqrfound.screen"));
            screenshot = null;

            if (!text.StartsWith("otpauth", StringComparison.Ordinal))
            {
                throw new Exception(I18n.GetString("import.invalidqr.screen"));
            }

            return OtpTextImporter.ImportUri(text);
        }
    }
}
