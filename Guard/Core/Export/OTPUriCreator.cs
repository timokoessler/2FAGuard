using Guard.Core.Models;

namespace Guard.Core.Export
{
    internal class OTPUriCreator
    {
        internal static string GetUri(TOTPTokenHelper token)
        {
            DBTOTPToken dBToken = token.dBToken;
            string uri = $"otpauth://totp/{Uri.EscapeDataString(dBToken.Issuer)}";

            if (dBToken.Username != null)
            {
                uri += $":{Uri.EscapeDataString(dBToken.Username)}";
            }

            uri += $"?secret={token.decryptedSecret}";
            uri += $"&issuer={Uri.EscapeDataString(dBToken.Issuer)}";

            if (dBToken.Algorithm != null)
            {
                uri += $"&algorithm={dBToken.Algorithm.ToString()!.ToLower()}";
            }

            if (dBToken.Digits != null)
            {
                uri += $"&digits={dBToken.Digits}";
            }

            if (dBToken.Period != null)
            {
                uri += $"&period={dBToken.Period}";
            }
            return uri;
        }
    }
}
