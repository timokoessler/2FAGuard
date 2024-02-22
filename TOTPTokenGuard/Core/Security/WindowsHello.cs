using Windows.Security.Credentials;
using Windows.Security.Credentials.UI;
using Windows.Security.Cryptography;

namespace TOTPTokenGuard.Core.Security
{
    internal class WindowsHello
    {
        private static readonly string accountName = "TOTPTokenGuard";

        public static async Task<KeyCredentialRetrievalResult> Register()
        {
            return await KeyCredentialManager.RequestCreateAsync(
                accountName,
                KeyCredentialCreationOption.FailIfExists
            );
        }

        public static async Task<bool> IsAvailable()
        {
            return await KeyCredentialManager.IsSupportedAsync();
        }

        public static async Task<bool> RequestSimpleVerification()
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
        public static async Task<string> GetSignedChallenge()
        {
            var openKeyResult = await KeyCredentialManager.OpenAsync(accountName);
            if (openKeyResult.Status != KeyCredentialStatus.Success)
            {
                throw new Exception(
                    $"Failed to authenticate with Windows Hello: {openKeyResult.Status}"
                );
            }
            return await SignChallenge(
                openKeyResult.Credential,
                Auth.GetWindowsHelloChallenge()
            );
        }

        public static async Task<string> SignChallenge(KeyCredential credential, string challenge)
        {
            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }
            var buffer = CryptographicBuffer.ConvertStringToBinary(
                challenge,
                BinaryStringEncoding.Utf8
            );
            var result = await credential.RequestSignAsync(buffer);
            if (result.Status != KeyCredentialStatus.Success)
            {
                throw new Exception($"Failed to sign Windows Hello challenge: {result.Status}");
            }
            return CryptographicBuffer.EncodeToBase64String(result.Result);
        }

        public static async Task Unregister()
        {
            await KeyCredentialManager.DeleteAsync(accountName);
        }
    }
}
