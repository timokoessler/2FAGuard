using Guard.Core.Models;
using Guard.Core.Security.WebAuthn.entities;

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
                return (false, "WebAuthn API is not available on this platform.");
            }

            var challenge = EncryptionHelper.GetRandomBytes(32);
            var extensions = new List<WebAuthnCreationExtensionInput>
            {
                new HmacSecretCreationExtension()
            };

            var webauthnDevices = Auth.GetWebAuthnDevices();
            List<Credential> excludeCredentials = [];
            foreach (var device in webauthnDevices)
            {
                byte[] id = Convert.FromBase64String(device.Id);
                excludeCredentials.Add(new Credential(id));
            }

            WindowsHello.FocusSecurityPrompt();

            return await Task.Run<(bool success, string? error)>(async () =>
            {
                /*
                 * ToDo:
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
                        UserVerificationRequirement = UserVerificationRequirement.Required,
                        ExcludeCredentials = excludeCredentials,
                    },
                    out var credential
                );

                if (res != WebAuthnHResult.Ok)
                {
                    return (false, res.ToString());
                }

                if (credential == null)
                {
                    return (false, "Credential is null");
                }

                var device = new WebauthnDevice()
                {
                    Id = Convert.ToBase64String(credential.CredentialId),
                    EncryptedName = null,
                    ProtectedKey = "",
                    Salt1 = EncryptionHelper.GetRandomBase64String(32),
                    Salt2 = EncryptionHelper.GetRandomBase64String(32),
                };

                var assertion = await Assert(windowHandle, [device]);
                if (!assertion.success)
                {
                    return (false, "Assertion failed: " + assertion.error);
                }

                if (assertion.result == null)
                {
                    return (false, "Assertion result is null");
                }

                if (!assertion.result.CredentialId.SequenceEqual(credential.CredentialId))
                {
                    return (false, "CredentialId does not match");
                }

                // https://fidoalliance.org/specs/fido-v2.1-ps-20210615/fido-client-to-authenticator-protocol-v2.1-ps-20210615.html#sctn-hmac-secret-extension
                await Auth.AddWebAuthnDevice(device, assertion.result);

                return (true, null);
            });
        }

        public static async Task<(bool success, string? error, AssertionResult? result)> Assert(
            IntPtr windowHandle,
            List<WebauthnDevice>? webauthnDevices = null
        )
        {
            if (!IsSupported())
            {
                return (false, "WebAuthn API is not available on this platform.", null);
            }

            if (webauthnDevices == null)
            {
                webauthnDevices = Auth.GetWebAuthnDevices();
            }

            var challenge = EncryptionHelper.GetRandomBytes(32);

            var saltMap = new Dictionary<byte[], PrfSalt>();
            List<Credential> allowedCredentials = [];
            foreach (var device in webauthnDevices)
            {
                byte[] id = Convert.FromBase64String(device.Id);
                allowedCredentials.Add(new Credential(id));
                saltMap.Add(
                    id,
                    new PrfSalt()
                    {
                        First = Convert.FromBase64String(device.Salt1),
                        Second = Convert.FromBase64String(device.Salt2)
                    }
                );
            }

            var extensions = new List<WebAuthnAssertionExtensionInput>
            {
                new HmacSecretAssertionExtension() { SaltsByCredential = saltMap, }
            };

            WindowsHello.FocusSecurityPrompt();

            return await Task.Run<(bool success, string? error, AssertionResult? result)>(() =>
            {
                var res = WebAuthnInterop.GetAssertion(
                    windowHandle,
                    WebAuthnSettings.Origin,
                    WebAuthnSettings.GetClientData(challenge, WebAuthnSettings.ClientDataType.Get),
                    new AuthenticatorGetAssertionOptions
                    {
                        AllowedCredentials = allowedCredentials,
                        UserVerificationRequirement = UserVerificationRequirement.Required,
                        Extensions = extensions,
                        AuthenticatorAttachment = AuthenticatorAttachment.CrossPlatform,
                    },
                    out var assertion
                );

                if (res != WebAuthnHResult.Ok)
                {
                    return (false, res.ToString(), null);
                }

                if (assertion == null)
                {
                    return (false, "Assertion is null", null);
                }

                if (
                    assertion.HmacSecret == null
                    || assertion.HmacSecret.First == null
                    || assertion.HmacSecret.Second == null
                )
                {
                    return (false, "HmacSecret is null", null);
                }

                if (
                    assertion.HmacSecret.First.Length != 32
                    || assertion.HmacSecret.Second.Length != 32
                )
                {
                    return (false, "HmacSecret length is invalid", null);
                }

                if (
                    !allowedCredentials.Any(c =>
                        c.CredentialId.SequenceEqual(assertion.Credential.CredentialId)
                    )
                )
                {
                    return (false, "CredentialId is not in allowedCredentials", null);
                }

                byte[] hmacSecret = new byte[64];
                Array.Copy(assertion.HmacSecret.First, 0, hmacSecret, 0, 32);
                Array.Copy(assertion.HmacSecret.Second, 0, hmacSecret, 32, 32);

                return (
                    true,
                    null,
                    new AssertionResult()
                    {
                        CredentialId = assertion.Credential.CredentialId,
                        HmacSecret = hmacSecret
                    }
                );
            });
        }
    }

    public class AssertionResult
    {
        public required byte[] CredentialId { get; set; }
        public required byte[] HmacSecret { get; set; }
    }
}
