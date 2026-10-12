using System.Text;
using Guard.Linux.Core;
using Guard.Linux.Core.Import;

namespace Guard.WPF.Core.Import.Importer
{
    internal class ClipboardImporter : IImporter
    {
        public string Name => "Clipboard";
        public IImporter.ImportType Type => IImporter.ImportType.Clipboard;
        public string SupportedFileExtensions => "";

        public bool RequiresPassword(string? path) => false;

        private static readonly string[] imageExtensions = [".png", ".jpg", ".jpeg"];
        private static readonly string[] imageFormats = ["image/png", "image/jpeg", "image/bmp"];

        private string? text;
        private byte[]? imageData;
        private List<string>? files;

        /// <summary>
        /// Reads the clipboard content. Must be called on the UI thread before Parse.
        /// </summary>
        internal async Task LoadClipboard()
        {
            string[] formats = await ClipboardHelper.GetFormats();

            string? imageFormat = imageFormats.FirstOrDefault(formats.Contains);
            if (imageFormat != null)
            {
                imageData = await ClipboardHelper.GetBytes(imageFormat);
                if (imageData != null)
                {
                    return;
                }
            }

            if (formats.Contains("text/uri-list"))
            {
                byte[]? uriList = await ClipboardHelper.GetBytes("text/uri-list");
                if (uriList != null)
                {
                    files = Encoding.UTF8.GetString(uriList)
                        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                        .Where(line => !line.StartsWith('#'))
                        .Select(line => line.StartsWith("file://", StringComparison.Ordinal) ? new Uri(line).LocalPath : line)
                        .ToList();
                    if (files.Count > 0)
                    {
                        return;
                    }
                    files = null;
                }
            }

            text = (await ClipboardHelper.GetText())?.Trim();
        }

        public (int total, int duplicate, int tokenID) Parse(string? path, byte[]? password)
        {
            string? content = null;
            if (imageData != null)
            {
                content =
                    QRCode.ParseQRImage(imageData)
                    ?? throw new Exception(I18n.GetString("import.noqrfound"));
            }
            else if (files != null)
            {
                if (files.Count != 1)
                {
                    throw new Exception(I18n.GetString("import.clipboard.invalid.multifiles"));
                }
                string file = files[0];
                if (!imageExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new Exception(I18n.GetString("import.clipboard.invalid"));
                }
                content =
                    QRCode.ParseQRFile(file)
                    ?? throw new Exception(I18n.GetString("import.noqrfound"));
            }
            else if (text != null)
            {
                if (!text.StartsWith("otpauth://", StringComparison.Ordinal)
                    && !text.StartsWith("otpauth-migration:", StringComparison.Ordinal))
                {
                    throw new Exception(I18n.GetString("import.clipboard.invalid"));
                }
                content = text;
            }

            if (content == null)
            {
                throw new Exception(I18n.GetString("import.clipboard.invalid"));
            }

            return OtpTextImporter.ImportUri(content);
        }
    }
}
