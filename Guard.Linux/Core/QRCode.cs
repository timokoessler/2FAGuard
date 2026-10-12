using Avalonia.Media.Imaging;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace Guard.WPF.Core
{
    internal class QRCode
    {
        public static string? ParseQRBitmap(SKBitmap bitmap)
        {
            using SKBitmap rgba =
                bitmap.ColorType == SKColorType.Rgba8888
                    ? bitmap.Copy()
                    : bitmap.Copy(SKColorType.Rgba8888);
            byte[] pixels = rgba.Bytes;
            var source = new RGBLuminanceSource(
                pixels,
                rgba.Width,
                rgba.Height,
                RGBLuminanceSource.BitmapFormat.RGBA32
            );
            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = { PossibleFormats = [BarcodeFormat.QR_CODE], TryHarder = true },
            };
            return reader.Decode(source)?.Text;
        }

        public static string? ParseQRFile(string filePath)
        {
            using SKBitmap? bitmap = SKBitmap.Decode(filePath);
            if (bitmap == null)
            {
                return null;
            }
            return ParseQRBitmap(bitmap);
        }

        public static string? ParseQRImage(byte[] imageData)
        {
            using SKBitmap? bitmap = SKBitmap.Decode(imageData);
            if (bitmap == null)
            {
                return null;
            }
            return ParseQRBitmap(bitmap);
        }

        /// <summary>
        /// Returns a PNG encoded QR code
        /// </summary>
        public static byte[] GenerateQRCodePng(string text, int size)
        {
            BitMatrix matrix = new QRCodeWriter().encode(
                text,
                BarcodeFormat.QR_CODE,
                size,
                size,
                new Dictionary<EncodeHintType, object> { { EncodeHintType.MARGIN, 1 } }
            );

            using SKBitmap bitmap = new(matrix.Width, matrix.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            for (int y = 0; y < matrix.Height; y++)
            {
                for (int x = 0; x < matrix.Width; x++)
                {
                    bitmap.SetPixel(x, y, matrix[x, y] ? SKColors.Black : SKColors.White);
                }
            }
            using SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        public static Bitmap GenerateQRCodeImage(string text, int size)
        {
            using MemoryStream stream = new(GenerateQRCodePng(text, size));
            return new Bitmap(stream);
        }
    }
}
