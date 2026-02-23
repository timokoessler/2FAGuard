using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Windows.Compatibility;

namespace Guard.WPF.Core
{
    internal class QRCode
    {
        public static string? ParseQRBitmap(Bitmap bitmap)
        {
            BarcodeReader reader = new()
            {
                AutoRotate = true,
                Options = { PossibleFormats = [BarcodeFormat.QR_CODE] },
            };

            var result = reader.Decode(bitmap);

            if (result != null)
            {
                return result.Text;
            }
            return null;
        }

        public static string? ParseQRFile(string filePath)
        {
            using Bitmap barcodeBitmap = (Bitmap)Image.FromFile(filePath);
            return ParseQRBitmap(barcodeBitmap);
        }

        public static Bitmap GenerateQRCode(string text, ZXing.Common.EncodingOptions? options)
        {
            options ??= new ZXing.Common.EncodingOptions();
            var writer = new BarcodeWriter { Format = BarcodeFormat.QR_CODE, Options = options };
            return writer.Write(text);
        }

        public static BitmapImage GenerateQRCodeImage(string text, int size)
        {
            using Bitmap bitmap = GenerateQRCode(
                text,
                new ZXing.Common.EncodingOptions { Width = size, Height = size }
            );
            using MemoryStream memory = new();
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            return bitmapImage;
        }
    }
}
