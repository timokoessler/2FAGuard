using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Guard.Core.Models;

namespace Guard.Core.Import.Importer
{
    internal class ClipboardImporter : IImporter
    {
        public string Name => "Clipboard";
        public IImporter.ImportType Type => IImporter.ImportType.Clipboard;
        public string SupportedFileExtensions => "";

        public bool RequiresPassword(string? path) => false;

        public (int total, int duplicate, int tokenID) Parse(string? path, string? password)
        {
            string? text = null;
            if (Clipboard.ContainsText())
            {
                text = Clipboard.GetText();
                if (text == null || !text.StartsWith("otpauth://"))
                {
                    throw new Exception(I18n.GetString("import.clipboard.invalid"));
                }
            }
            else if (Clipboard.ContainsImage())
            {
                BitmapSource bitmapSource =
                    Clipboard.GetImage()
                    ?? throw new Exception("Could not get image from clipboard");
                Bitmap bitmap =
                    BitmapSourceToBitmap(bitmapSource)
                    ?? throw new Exception("Could not convert BitmapSource to Bitmap");

                text =
                    QRCode.ParseQRBitmap(bitmap)
                    ?? throw new Exception(I18n.GetString("import.noqrfound"));
            }
            else if (Clipboard.ContainsFileDropList())
            {
                var fileDropList = Clipboard.GetFileDropList();
                // Todo Add support for multiple files?
                if (fileDropList.Count != 1)
                {
                    throw new Exception(I18n.GetString("import.clipboard.invalid.multifiles"));
                }
                var file = fileDropList[0];
                if (
                    file == null
                    || !file.EndsWith(".png") && !file.EndsWith(".jpg") && !file.EndsWith(".jpeg")
                )
                {
                    throw new Exception(I18n.GetString("import.clipboard.invalid"));
                }
                text =
                    QRCode.ParseQRFile(file)
                    ?? throw new Exception(I18n.GetString("import.noqrfound"));
            }

            if (text == null)
            {
                throw new Exception(I18n.GetString("import.clipboard.invalid"));
            }

            if (text.StartsWith("otpauth-migration:"))
            {
                int duplicateTokens = 0;
                List<OTPUri> otpUris = GoogleAuthenticator.Parse(text);
                if (otpUris.Count == 0)
                {
                    throw new Exception(
                        "Google Authenticator migration failed because no tokens were found."
                    );
                }
                foreach (OTPUri gOTPUri in otpUris)
                {
                    DBTOTPToken gDBToken = OTPUriParser.ConvertToDBToken(gOTPUri);
                    if (!TokenManager.AddToken(gDBToken))
                    {
                        duplicateTokens++;
                    }
                }
                return (otpUris.Count, duplicateTokens, 0);
            }

            OTPUri otpUri = OTPUriParser.Parse(text);
            DBTOTPToken dbToken = OTPUriParser.ConvertToDBToken(otpUri);

            if (!TokenManager.AddToken(dbToken))
            {
                return (1, 1, 0);
            }

            return (1, 0, dbToken.Id);
        }

        private static Bitmap BitmapSourceToBitmap(BitmapSource bitmapSource)
        {
            Bitmap bitmap;
            using (MemoryStream outStream = new())
            {
                // Create encoder
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                // Save BitmapSource to stream
                encoder.Save(outStream);
                // Create Bitmap from stream
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }
    }
}
