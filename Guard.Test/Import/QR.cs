using Guard.WPF.Core;

namespace Guard.Test.Import
{
    public class QR
    {
        [Fact]
        public void ParseGoogleMigrationQR()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "ScreenshotGAuthenticator.jpg"
            );

            string uri =
                "otpauth-migration://offline?data=Ci0KEBfNhW48%2BTYHuJJjaGSVaDgSDHRlc3RAdGVzdC5kZRoFQXBwbGUgASgBMAIKLwoUULGGAD3E0ueYHAy7s69SLeV1tHISCXVzZXJAdGVzdBoGVG9rZW4yIAEoATACEAEYASAAKJbEnKb%2B%2F%2F%2F%2F%2FwE%3D";

            string? result = QRCode.ParseQRFile(path);
            Assert.Equal(uri, result);
        }

        // Downscaled dense QR codes that ZXing cannot decode at their original size (#116)
        [Theory]
        [InlineData("QRDownscaled4.png")]
        [InlineData("QRDownscaled10.png")]
        public void ParseDownscaledDenseQR(string fileName)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);

            string? result = QRCode.ParseQRFile(path);
            Assert.StartsWith("otpauth-migration://offline?data=", result);
        }
    }
}
