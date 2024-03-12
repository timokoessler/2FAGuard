using Guard.Core.Icons;
using Guard.Core.Models;
using Guard.Core.Security;
using OtpNet;

namespace Guard.Core.Import
{
    /// <summary>
    /// https://github.com/google/google-authenticator/wiki/Key-Uri-Format
    /// </summary>
    class OTPUriParser
    {
        internal static OTPUri Parse(string uriString)
        {
            ArgumentNullException.ThrowIfNull(uriString);

            Uri uri = new(uriString);
            if (uri.Scheme != "otpauth")
            {
                throw new Exception("Invalid URI scheme");
            }
            if (uri.Host == "hotp")
            {
                throw new Exception("HOTP is not supported");
            }

            if (uri.Host != "totp")
            {
                if (uri.Host == "hotp")
                {
                    throw new Exception(I18n.GetString("import.hotp.notsupported"));
                }
                throw new Exception("Invalid URI host");
            }

            OTPUri otpUri = new();

            string label = uri.LocalPath;

            if (label.Contains(':'))
            {
                otpUri.Issuer = label.Split(':')[0][1..];
                otpUri.Account = label.Split(':')[1];
            }
            else if (label.Contains("%3A"))
            {
                otpUri.Issuer = label.Split("%3A")[0][1..];
                otpUri.Account = label.Split("%3A")[1];
            }

            if (uri.Query == null || uri.Query.Length < 2)
            {
                throw new Exception("Invalid URI query");
            }

            string[] query = uri.Query[1..].Split('&');
            foreach (string q in query)
            {
                string[] kv = q.Split('=');
                if (kv.Length != 2)
                {
                    throw new Exception("Invalid URI query");
                }
                string key = kv[0].ToLower();
                if (key == "secret")
                {
                    otpUri.Secret = kv[1];
                }
                else if (key == "issuer")
                {
                    otpUri.Issuer = Uri.UnescapeDataString(kv[1]);
                }
                else if (key == "algorithm")
                {
                    TOTPAlgorithm algorithm = kv[1].ToUpper() switch
                    {
                        "SHA1" => TOTPAlgorithm.SHA1,
                        "SHA256" => TOTPAlgorithm.SHA256,
                        "SHA512" => TOTPAlgorithm.SHA512,
                        _ => throw new Exception("Invalid algorithm")
                    };
                    otpUri.Algorithm = algorithm;
                }
                else if (key == "digits")
                {
                    otpUri.Digits = int.Parse(kv[1]);
                    if (otpUri.Digits < 6 || otpUri.Digits > 8)
                    {
                        throw new Exception("Invalid digits count");
                    }
                }
                else if (key == "period")
                {
                    otpUri.Period = int.Parse(kv[1]);
                    if (otpUri.Period < 1)
                    {
                        throw new Exception("Invalid period");
                    }
                }
            }

            if (otpUri.Issuer == null)
            {
                throw new Exception($"Missing issuer in URI");
            }

            if (otpUri.Secret == null)
            {
                throw new Exception($"Missing secret in URI from {otpUri.Issuer}");
            }

            try
            {
                Base32Encoding.ToBytes(otpUri.Secret);
            }
            catch
            {
                throw new Exception(I18n.GetString("td.invalidsecret"));
            }

            return otpUri;
        }

        internal static DBTOTPToken ConvertToDBToken(OTPUri otpUri)
        {
            ArgumentNullException.ThrowIfNull(otpUri);
            ArgumentNullException.ThrowIfNull(otpUri.Issuer);
            ArgumentNullException.ThrowIfNull(otpUri.Secret);

            IconManager.TotpIcon icon = IconManager.GetIcon(
                otpUri.Issuer,
                IconManager.IconColor.Colored,
                IconManager.IconType.Any
            );

            DBTOTPToken dbToken =
                new()
                {
                    Id = TokenManager.GetNextId(),
                    Issuer = otpUri.Issuer,
                    EncryptedSecret = Auth.GetMainEncryptionHelper().EncryptString(otpUri.Secret),
                    Algorithm = otpUri.Algorithm,
                    Digits = otpUri.Digits,
                    Period = otpUri.Period,
                    Username = otpUri.Account,
                    CreationTime = DateTime.Now
                };

            if (icon != null && icon.Type != IconManager.IconType.Default)
            {
                dbToken.Icon = icon.Name;
                dbToken.IconType = icon.Type;
            }

            return dbToken;
        }
    }
}
