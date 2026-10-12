using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;
using ZXing.Common;
using ZXing.ImageSharp;

namespace Guard.Core
{
    public class QRCode
    {
        public static string? ParseQRBitmap(Image<Rgba32> image)
        {
            var barcodeBitmap = new ZXing.ImageSharp.BarcodeReader<Rgba32>
            {
                AutoRotate = true,
                Options = new DecodingOptions { PossibleFormats = [BarcodeFormat.QR_CODE] },
            };

            var result = barcodeBitmap.Decode(image);

            if (result != null)
            {
                return result.Text;
            }
            return null;
        }

        public static string? ParseQRFile(string filePath)
        {
            using var image = Image.Load<Rgba32>(filePath);
            return ParseQRBitmap(image);
        }

        public static Image<Rgba32> GenerateQRCode(string text, EncodingOptions? options = null)
        {
            options ??= new EncodingOptions();
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = options
            };
            var pixelData = writer.Write(text);
            var image = new Image<Rgba32>(pixelData.Width, pixelData.Height);
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < pixelData.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    var sourceRow = pixelData.Pixels.AsSpan(y * pixelData.Width * 4, pixelData.Width * 4);
                    for (int x = 0; x < pixelData.Width; x++)
                    {
                        var pixel = sourceRow.Slice(x * 4, 4);
                        row[x] = new Rgba32(pixel[0], pixel[1], pixel[2], pixel[3]);
                    }
                }
            });
            return image;
        }

        public static byte[] GenerateQRCodePng(string text, int size)
        {
            using var image = GenerateQRCode(
                text,
                new EncodingOptions { Width = size, Height = size }
            );
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return ms.ToArray();
        }
    }
}