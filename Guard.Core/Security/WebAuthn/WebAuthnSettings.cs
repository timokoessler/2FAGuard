using System.Text;
using System.Text.Json;
using Guard.Core.Security.WebAuthn.entities;
using NeoSmart.Utils;

namespace Guard.Core.Security.WebAuthn
{
    internal class WebAuthnSettings
    {
        public static string Origin = "2faguard.app";

        public static RelayingPartyInfo RelayingPartyInfo
        {
            get => new() { Id = Origin, Name = "2FAGuard", };
        }

        public static UserInfo UserInfo
        {
            get =>
                new()
                {
                    UserId = Encoding.UTF8.GetBytes(Auth.GetInstallationID()),
                    Name = "2FAGuard User",
                    DisplayName = "2FAGuard User"
                };
        }

        public static List<CoseCredentialParameter> CoseCredentialParameters
        {
            get =>
                new()
                {
                    new CoseCredentialParameter
                    {
                        Algorithm = CoseAlgorithm.ECDSA_P521_WITH_SHA512
                    },
                    new CoseCredentialParameter
                    {
                        Algorithm = CoseAlgorithm.ECDSA_P384_WITH_SHA384
                    },
                    new CoseCredentialParameter { Algorithm = CoseAlgorithm.EDDSA },
                    new CoseCredentialParameter
                    {
                        Algorithm = CoseAlgorithm.ECDSA_P256_WITH_SHA256
                    },
                    new CoseCredentialParameter
                    {
                        Algorithm = CoseAlgorithm.RSASSA_PKCS1_V1_5_WITH_SHA512,
                    },
                    new CoseCredentialParameter { Algorithm = CoseAlgorithm.RSA_PSS_WITH_SHA512 },
                };
        }

        public enum ClientDataType
        {
            Create,
            Get
        }

        public static ClientData GetClientData(byte[] challenge, ClientDataType type)
        {
            string typeString = type switch
            {
                ClientDataType.Create => "webauthn.create",
                ClientDataType.Get => "webauthn.get",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
            return new ClientData()
            {
                ClientDataJSON = JsonSerializer.SerializeToUtf8Bytes(
                    new
                    {
                        type = typeString,
                        challenge = UrlBase64.Encode(challenge),
                        origin = Origin
                    }
                ),
                HashAlgorithm = HashAlgorithm.Sha512
            };
        }
    }
}
