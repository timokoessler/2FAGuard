using Guard.Core.Platform;
using Guard.Core.Security.WebAuthn.entities;

namespace Guard.Core.Security.WebAuthn
{
    internal static class WebAuthnInterop
    {
        public static bool IsAvailable()
        {
            try
            {
                return PlatformService.Instance is not FallbackPlatformService;
            }
            catch
            {
                return false;
            }
        }

        public static bool CheckApiAvailable()
        {
            // On non-Windows platforms, WebAuthn is not available via this API
            return false;
        }

        public static int GetApiVersion()
        {
            return 0;
        }

        public static async Task<PlatformCredentialAttestation?> CreateCredentialAsync(
            IntPtr window,
            string rpId,
            string userName,
            string userDisplayName)
        {
            // On Linux/macOS, we would use platform-specific WebAuthn implementations
            // For now, return null as it requires native interop
            return await Task.FromResult<PlatformCredentialAttestation?>(null);
        }

        public static async Task<PlatformAssertion?> GetAssertionAsync(
            IntPtr window,
            string rpId)
        {
            // On Linux/macOS, we would use platform-specific WebAuthn implementations
            // For now, return null as it requires native interop
            return await Task.FromResult<PlatformAssertion?>(null);
        }

        // Legacy methods for backward compatibility with WebAuthnHelper (Windows only)
        public static WebAuthnHResult CreateCredential(
            IntPtr window,
            RelayingPartyInfo rp,
            UserInfo user,
            ICollection<CoseCredentialParameter> coseParams,
            ClientData clientData,
            AuthenticatorMakeCredentialOptions makeOptions,
            out CredentialAttestation? credential)
        {
            credential = null;
            return WebAuthnHResult.NotSupported;
        }

        public static WebAuthnHResult GetAssertion(
            IntPtr window,
            string rpId,
            ClientData clientData,
            AuthenticatorGetAssertionOptions getOptions,
            out Assertion? assertion)
        {
            assertion = null;
            return WebAuthnHResult.NotSupported;
        }
    }
}