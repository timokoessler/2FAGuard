using Guard.Core.Models;

namespace Guard.WPF.Core.Export
{
    internal class OTPUriCreator
    {
        internal static string GetUri(TOTPTokenHelper token)
        {
            DBTOTPToken dBToken = token.dBToken;
            string uri = $"otpauth://totp/{Uri.EscapeDataString(dBToken.Issuer)}";

            if (token.Username != null)
            {
                uri += $":{Uri.EscapeDataString(token.Username)}";
            }

            uri += $"?secret={token.DecryptedSecret}";
            uri += $"&issuer={Uri.EscapeDataString(dBToken.Issuer)}";

            if (dBToken.Algorithm != null)
            {
                uri += $"&algorithm={dBToken.Algorithm.ToString()!.ToLowerInvariant()}";
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
