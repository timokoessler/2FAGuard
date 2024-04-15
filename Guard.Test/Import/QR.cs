using Guard.Core;
using Guard.Core.Import.Importer;

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
    }
}
