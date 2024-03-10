using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Guard.Core.Models;
using Microsoft.Win32;

namespace Guard.Core.Import
{
    internal class QRFileImporter : IImporter
    {
        public string Name => "QRFile";
        public IImporter.ImportType Type => IImporter.ImportType.File;
        public string SupportedFileExtensions =>
            "QR-Code (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png";

        public (int total, int duplicate, int tokenID) Parse(string? path)
        {
            ArgumentNullException.ThrowIfNull(path);

            string? text =
                QRCode.ParseQRFile(path) ?? throw new Exception(I18n.GetString("import.noqrfound"));

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
                    DBTOTPToken gDBToken = OTPUriHelper.ConvertToDBToken(gOTPUri);
                    if (!TokenManager.AddToken(gDBToken))
                    {
                        duplicateTokens++;
                    }
                }
                return (otpUris.Count, duplicateTokens, 0);
            }

            OTPUri otpUri = OTPUriHelper.Parse(text);
            DBTOTPToken dbToken = OTPUriHelper.ConvertToDBToken(otpUri);

            if (!TokenManager.AddToken(dbToken))
            {
                return (1, 1, 0);
            }

            return (1, 0, dbToken.Id);
        }
    }
}
