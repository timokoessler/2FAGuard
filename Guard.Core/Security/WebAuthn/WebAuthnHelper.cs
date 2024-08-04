using System.Text;
using System.Text.Json;
using Guard.Core.Security.WebAuthn.entities;
using NeoSmart.Utils;
using OtpNet;

namespace Guard.Core.Security.WebAuthn
{
    public static class WebAuthnHelper
    {
        public static bool IsSupported()
        {
            return WebAuthnInterop.CheckApiAvailable();
        }

        public static int GetApiVersion()
        {
            return WebAuthnInterop.GetApiVersion();
        }

        public static void Register(IntPtr windowHandle)
        {
            if (!IsSupported())
            {
                throw new PlatformNotSupportedException(
                    "WebAuthn API is not available on this platform."
                );
            }

            RelayingPartyInfo relayingPartyInfo =
                new() { Id = "win.2faguard.app", Name = "2FAGuard", };

            UserInfo userInfo =
                new()
                {
                    UserId = Encoding.UTF8.GetBytes(Auth.GetInstallationID()),
                    Name = "2FAGuard",
                    DisplayName = "2FAGuard"
                };

            List<CoseCredentialParameter> coseCredentialParameters =
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
                    new CoseCredentialParameter
                    {
                        Algorithm = CoseAlgorithm.RSASSA_PKCS1_V1_5_WITH_SHA384,
                    },
                    new CoseCredentialParameter { Algorithm = CoseAlgorithm.RSA_PSS_WITH_SHA384 },
                    new CoseCredentialParameter
                    {
                        Algorithm = CoseAlgorithm.RSASSA_PKCS1_V1_5_WITH_SHA256,
                    },
                    new CoseCredentialParameter { Algorithm = CoseAlgorithm.RSA_PSS_WITH_SHA256 },
                };

            var challenge = EncryptionHelper.GetRandomBytes(32);

            var extensions = new List<WebAuthnCreationExtensionInput>();

            /*
             * ToDo:
             * - excludeCredentials
             * - authenticatorSelection
             * - PreferResidentKey
             * - cancellation
             * - credential protection
             */
            var res = WebAuthnInterop.AuthenticatorMakeCredential(
                windowHandle,
                relayingPartyInfo,
                userInfo,
                coseCredentialParameters,
                new ClientData()
                {
                    ClientDataJSON = JsonSerializer.SerializeToUtf8Bytes(
                        new
                        {
                            type = "webauthn.create",
                            challenge = UrlBase64.Encode(challenge),
                            origin = "win.2faguard.app"
                        }
                    ),
                    HashAlgorithm = HashAlgorithm.Sha512
                },
                new AuthenticatorMakeCredentialOptions
                {
                    AuthenticatorAttachment = AuthenticatorAttachment.CrossPlatform,
                    Extensions = extensions,
                    UserVerificationRequirement = UserVerificationRequirement.Preferred
                },
                out var credential
            );

            if (res != WebAuthnHResult.Ok)
            {
                throw new Exception($"Failed to register WebAuthn credential: {res}");
            }

            Log.Logger.Information(
                "WebAuthn credential registered successfully: {credential}",
                credential
            );
        }
    }
}
