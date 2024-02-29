using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Guard.Core.Models;
using Guard.Views.Pages;

namespace Guard.Core.Import
{
    internal class ClipboardImport
    {
        internal static DBTOTPToken? Parse()
        {
            string? text = null;
            if (Clipboard.ContainsText())
            {
                text = Clipboard.GetText();
                if (text == null || !text.StartsWith("otpauth://"))
                {
                    return null;
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
                    || (!file.EndsWith(".png") && !file.EndsWith(".jpg") && !file.EndsWith(".jpeg"))
                )
                {
                    return null;
                }
                text =
                    QRCode.ParseQRFile(file)
                    ?? throw new Exception(I18n.GetString("import.noqrfound"));
            }

            if (text == null)
            {
                return null;
            }

            OTPUri otpUri = OTPUriHelper.Parse(text);
            DBTOTPToken dbToken = OTPUriHelper.ConvertToDBToken(otpUri);
            TokenManager.AddToken(dbToken);
            return dbToken;
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
