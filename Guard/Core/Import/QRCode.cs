using System.Drawing;
using ZXing;
using ZXing.Windows.Compatibility;

namespace Guard.Core.Import
{
    internal class QRCode
    {
        public static string? ParseQRBitmap(Bitmap bitmap)
        {
            BarcodeReader reader =
                new()
                {
                    AutoRotate = true,
                    Options = { PossibleFormats = [BarcodeFormat.QR_CODE] }
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
    }
}
