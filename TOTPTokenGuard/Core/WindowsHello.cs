using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Security.Credentials.UI;

namespace TOTPTokenGuard.Core
{
    class WindowsHello
    {
        public static async Task<string> Register()
        {
            var keyCreationResult = await KeyCredentialManager.RequestCreateAsync(
                "TOTPTokenGuard",
                KeyCredentialCreationOption.FailIfExists
            );

            if (keyCreationResult.Status == KeyCredentialStatus.Success)
            {
                return "To-Do";
            }
            return "To-Do";
        }

        public static async Task<bool> IsAvailable()
        {
            return await KeyCredentialManager.IsSupportedAsync();
        }

        public static async Task<bool> RequestVerification()
        {
            UserConsentVerificationResult consentResult =
                await UserConsentVerifier.RequestVerificationAsync(
                    I18n.GetString("win.hello.request")
                );
            return consentResult == UserConsentVerificationResult.Verified;
        }

        /// <summary>
        /// Get a secret key by signing a payload with Windows Hello.
        /// Strategy is based on Bitwarden's Windows Hello implementation:
        /// https://github.com/bitwarden/clients/blob/1f8e6ea6f8a736a8a766e5e0643c7106adfa8433/apps/desktop/desktop_native/src/biometric/windows.rs#L69
        /// </summary>
        /// <returns>A string used as AES key</returns>
        public static async Task<string> GetEncryptionKey()
        {
            return "To-Do";
        }
    }
}
