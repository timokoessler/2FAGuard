using System.Drawing;
using System.Windows;
using Guard.WPF.Core.Models;

namespace Guard.WPF.Core.Import.Importer
{
    internal class QRScreenCaptureImporter : IImporter
    {
        public string Name => "QRScreenCapture";
        public IImporter.ImportType Type => IImporter.ImportType.ScreenCapture;
        public string SupportedFileExtensions => "";

        public bool RequiresPassword(string? path) => false;

        public (int total, int duplicate, int tokenID) Parse(string? path, byte[]? password)
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

            mainWindow.MinimizeWindow();

            Bitmap capture = ScreenCapture.CaptureAllScreens();

            mainWindow.RestoreWindow();

            string? text =
                QRCode.ParseQRBitmap(capture)
                ?? throw new Exception(I18n.GetString("import.noqrfound.screen"));

            if (!text.StartsWith("otpauth"))
            {
                throw new Exception(I18n.GetString("import.invalidqr.screen"));
            }

            // Google Authenticator
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
    }
}
