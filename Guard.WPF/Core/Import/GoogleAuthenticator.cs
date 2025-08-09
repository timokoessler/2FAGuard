using System.IO;
using Google.Protobuf;
using Guard.Core.Models;
using Guard.WPF.Core.Import.GoogleOTPMigration;
using OtpNet;

namespace Guard.WPF.Core.Import
{
    internal class GoogleAuthenticator
    {
        internal static List<OTPUri> Parse(string uriString)
        {
            Uri uri = new(uriString);
            if (uri.Scheme != "otpauth-migration")
            {
                throw new Exception(
                    "Cannot parse non-migration URI with GoogleAuthenticator class"
                );
            }
            string[] query = uri.Query[1..].Split('&');
            if (query.Length != 1)
            {
                throw new Exception("Invalid URI: Expected 1 query parameter (otpauth-migration)");
            }

            if (!query[0].StartsWith("data="))
            {
                throw new Exception(
                    "Invalid URI: Expected data query parameter (otpauth-migration)"
                );
            }

            string urlDecoded = Uri.UnescapeDataString(query[0][5..]);
            byte[] data = Convert.FromBase64String(urlDecoded);
            Stream stream = new MemoryStream(data);

            MigrationPayload migrationPayload = new();
            migrationPayload.MergeFrom(stream);

            List<OTPUri> oTPUris = [];
            foreach (var token in migrationPayload.OtpParameters)
            {
                if (string.IsNullOrEmpty(token.Issuer) && string.IsNullOrEmpty(token.Name))
                {
                    throw new Exception(
                        "Invalid token: Issuer and Name cannot be empty (otpauth-migration)"
                    );
                }
                if (token.Secret == null || token.Secret.Length == 0)
                {
                    throw new Exception(
                        "Invalid token: Secret cannot be empty (otpauth-migration)"
                    );
                }

                if (token.HasType)
                {
                    if (
                        token.Type == GoogleOTPMigration.OtpType.Hotp
                        || (
                            token.Type == GoogleOTPMigration.OtpType.Unspecified && token.HasCounter
                        )
                    )
                    {
                        throw new Exception(
                            "Invalid token: Only TOTP tokens are supported (otpauth-migration)"
                        );
                    }
                }

                OTPUri otpUri = new() { Type = OtpUriType.TOTP };
                if (string.IsNullOrEmpty(token.Issuer))
                {
                    otpUri.Issuer = "??????";
                }
                else
                {
                    otpUri.Issuer = token.Issuer;
                }
                if (token.HasAlgorithm)
                {
                    otpUri.Algorithm = token.Algorithm switch
                    {
                        Algorithm.Sha1 => TOTPAlgorithm.SHA1,
                        Algorithm.Sha256 => TOTPAlgorithm.SHA256,
                        Algorithm.Sha512 => TOTPAlgorithm.SHA512,
                        Algorithm.TypeUnspecified => TOTPAlgorithm.SHA1,
                        _ => throw new Exception(
                            "Invalid token: Invalid algorithm (otpauth-migration)"
                        ),
                    };
                }

                if (token.HasDigits)
                {
                    otpUri.Digits = token.Digits switch
                    {
                        DigitCount.Six => 6,
                        DigitCount.Eight => 8,
                        DigitCount.Unspecified => 6,
                        _ => throw new Exception(
                            "Invalid token: Invalid digits (otpauth-migration)"
                        ),
                    };
                }

                if (token.HasName)
                {
                    otpUri.Account = token.Name;
                }

                otpUri.Secret = Base32Encoding.ToString(token.Secret.ToByteArray());

                oTPUris.Add(otpUri);
            }
            return oTPUris;
        }
    }
}
