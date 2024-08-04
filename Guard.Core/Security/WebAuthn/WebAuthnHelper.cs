using System.Text;
using System.Text.Json;
using Guard.Core.Security.WebAuthn.entities;
using NeoSmart.Utils;

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

        public static async Task<(bool success, string? error)> Register(IntPtr windowHandle)
        {
            if (!IsSupported())
            {
                throw new PlatformNotSupportedException(
                    "WebAuthn API is not available on this platform."
                );
            }

            var challenge = EncryptionHelper.GetRandomBytes(32);
            var extensions = new List<WebAuthnCreationExtensionInput>
            {
                new HmacSecretCreationExtension()
            };

            WindowsHello.FocusSecurityPrompt();

            return await Task.Run<(bool success, string? error)>(() =>
            {
                /*
                 * ToDo:
                 * - excludeCredentials
                 * - authenticatorSelection
                 * - PreferResidentKey
                 * - cancellation
                 * - credential protection
                 */
                var res = WebAuthnInterop.CreateCredential(
                    windowHandle,
                    WebAuthnSettings.RelayingPartyInfo,
                    WebAuthnSettings.UserInfo,
                    WebAuthnSettings.CoseCredentialParameters,
                    WebAuthnSettings.GetClientData(
                        challenge,
                        WebAuthnSettings.ClientDataType.Create
                    ),
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
                    return (false, res.ToString());
                }

                // Todo check challenge


                Log.Logger.Information(
                    "WebAuthn credential registered successfully: {credential}",
                    credential
                );

                return (true, null);
            });
        }

        public static async Task<(bool success, string? error)> Assert(IntPtr windowHandle)
        {
            if (!IsSupported())
            {
                throw new PlatformNotSupportedException(
                    "WebAuthn API is not available on this platform."
                );
            }

            var challenge = EncryptionHelper.GetRandomBytes(32);

            var extensions = new List<WebAuthnAssertionExtensionInput>
            {
                new HmacSecretAssertionExtension()
            };

            WindowsHello.FocusSecurityPrompt();

            /*
             * Todos:
             * - AllowedCredentials
             */
            return await Task.Run<(bool success, string? error)>(() =>
            {
                var res = WebAuthnInterop.GetAssertion(
                    windowHandle,
                    WebAuthnSettings.Origin,
                    WebAuthnSettings.GetClientData(challenge, WebAuthnSettings.ClientDataType.Get),
                    new AuthenticatorGetAssertionOptions
                    {
                        UserVerificationRequirement = UserVerificationRequirement.Preferred
                    },
                    out var assertion
                );

                if (res != WebAuthnHResult.Ok)
                {
                    return (false, res.ToString());
                }

                // Todo check challenge

                Log.Logger.Information("WebAuthn assertion successful: {assertion}", assertion);

                return (true, null);
            });
        }
    }
}
