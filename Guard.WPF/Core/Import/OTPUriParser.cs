using Guard.Core.Models;
using Guard.Core.Security;
using Guard.WPF.Core.Icons;
using OtpNet;

namespace Guard.WPF.Core.Import
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

            if (uri.Host != "totp")
            {
                if (uri.Host == "hotp")
                {
                    throw new Exception(I18n.GetString("import.hotp.notsupported"));
                }
                throw new Exception("Invalid token type");
            }

            OTPUri otpUri = new() { Type = OtpUriType.TOTP, };

            string label = uri.LocalPath;
            if (label.Length < 2)
            {
                throw new Exception("Invalid URI label");
            }
            // Remove leading slash
            label = label[1..];

            string[] labelSplitChars = [":", "%3A", "/", "%2F"];

            foreach (string splitChar in labelSplitChars)
            {
                if (label.Contains(splitChar))
                {
                    var splitted = label.Split(splitChar);
                    if (splitted.Length == 2)
                    {
                        otpUri.Issuer = splitted[0];
                        otpUri.Account = splitted[1];
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(otpUri.Account))
            {
                otpUri.Account = label;
            }

            // Workaround for Cisco Meraki (issue #14)
            if (otpUri.Account.Equals("Meraki") && !string.IsNullOrEmpty(otpUri.Issuer))
            {
                otpUri.Account = otpUri.Issuer;
                otpUri.Issuer = "Meraki";
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

            if (string.IsNullOrEmpty(otpUri.Issuer))
            {
                throw new Exception($"Missing issuer in URI");
            }

            if (string.IsNullOrEmpty(otpUri.Secret))
            {
                throw new Exception($"Missing secret in URI from {otpUri.Issuer}");
            }

            string normalizedSecret = NormalizeSecret(otpUri.Secret);
            if (!IsValidSecret(normalizedSecret))
            {
                throw new Exception($"{I18n.GetString("td.invalidsecret")} ({otpUri.Issuer})");
            }
            otpUri.Secret = normalizedSecret;

            return otpUri;
        }

        internal static DBTOTPToken ConvertToDBToken(OTPUri otpUri)
        {
            ArgumentNullException.ThrowIfNull(otpUri);
            ArgumentNullException.ThrowIfNull(otpUri.Issuer);
            ArgumentNullException.ThrowIfNull(otpUri.Secret);

            TotpIcon icon = IconManager.GetIcon(otpUri.Issuer, IconType.Any);

            EncryptionHelper encryption = Auth.GetMainEncryptionHelper();

            DBTOTPToken dbToken =
                new()
                {
                    Id = TokenManager.GetNextId(),
                    Issuer = otpUri.Issuer,
                    EncryptedSecret = encryption.EncryptStringToBytes(otpUri.Secret),
                    Algorithm = otpUri.Algorithm,
                    Digits = otpUri.Digits,
                    Period = otpUri.Period,
                    CreationTime = DateTime.Now
                };

            if (otpUri.Account != null)
            {
                dbToken.EncryptedUsername = encryption.EncryptStringToBytes(otpUri.Account);
            }

            if (icon != null && icon.Type != IconType.Default)
            {
                dbToken.Icon = icon.Name;
                dbToken.IconType = icon.Type;
            }

            return dbToken;
        }

        internal static string NormalizeSecret(string secret)
        {
            return new string(
                secret.ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray()
            ).ToUpper();
        }

        internal static bool IsValidSecret(string secret)
        {
            try
            {
                Base32Encoding.ToBytes(secret);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
