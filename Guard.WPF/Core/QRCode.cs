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
        private static readonly double[] RetryScales = [1.5, 3];
        private const int MaxRetryWidth = 4000;

        public static string? ParseQRBitmap(Bitmap bitmap)
        {
            BarcodeReader reader = new()
            {
                AutoRotate = true,
                Options = { PossibleFormats = [BarcodeFormat.QR_CODE], TryHarder = true },
            };

            var result = reader.Decode(bitmap);

            foreach (double scale in RetryScales)
            {
                if (result != null || bitmap.Width * scale > MaxRetryWidth)
                {
                    break;
                }
                using Bitmap scaled = new(
                    bitmap,
                    (int)(bitmap.Width * scale),
                    (int)(bitmap.Height * scale)
                );
                result = reader.Decode(scaled);
            }

            return result?.Text;
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
