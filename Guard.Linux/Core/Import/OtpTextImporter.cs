using Guard.Core.Models;
using Guard.WPF.Core;
using Guard.WPF.Core.Import;

namespace Guard.Linux.Core.Import
{
    internal static class OtpTextImporter
    {
        /// <summary>
        /// Imports an otpauth:// or otpauth-migration:// uri
        /// </summary>
        internal static (int total, int duplicate, int tokenID) ImportUri(string text)
        {
            if (text.StartsWith("otpauth-migration:", StringComparison.Ordinal))
            {
                int duplicateTokens = 0;
                List<OTPUri> otpUris = GoogleAuthenticator.Parse(text);
                if (otpUris.Count == 0)
                {
                    throw new Exception(
                        "Google Authenticator migration failed because no tokens were found."
                    );
                }
                int lastAddedId = 0;
                foreach (OTPUri gOTPUri in otpUris)
                {
                    DBTOTPToken gDBToken = OTPUriParser.ConvertToDBToken(gOTPUri);
                    if (!TokenManager.AddToken(gDBToken))
                    {
                        duplicateTokens++;
                    }
                    else
                    {
                        lastAddedId = gDBToken.Id;
                    }
                }
                return (otpUris.Count, duplicateTokens, otpUris.Count == 1 ? lastAddedId : 0);
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
