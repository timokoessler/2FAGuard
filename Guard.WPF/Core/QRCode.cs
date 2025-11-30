using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;

namespace Guard.WPF.Core
{
    internal class QRCode
    {
        public static string? ParseQRBitmap(SKBitmap bitmap)
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
            SKBitmap barcodeBitmap = SKBitmap.Decode(filePath);
            return ParseQRBitmap(barcodeBitmap);
        }

        public static SKBitmap GenerateQRCode(string text, ZXing.Common.EncodingOptions? options)
        {
            options ??= new ZXing.Common.EncodingOptions();
            var writer = new BarcodeWriter { Format = BarcodeFormat.QR_CODE, Options = options };
            return writer.Write(text);
        }

        public static SKBitmap GenerateQRCodeImage(string text, int size)
        {
            SKBitmap bitmap = GenerateQRCode(
                text,
                new ZXing.Common.EncodingOptions { Width = size, Height = size }
            );

            return bitmap;
        }
    }
}
