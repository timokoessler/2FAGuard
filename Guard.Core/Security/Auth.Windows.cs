using System.Text;
using Guard.Core.Security.WebAuthn;
using Windows.Security.Credentials;

namespace Guard.Core.Security
{
    public partial class Auth
    {
        /// <summary>
        /// The password vault is used to store a encrypted version of the main encryption key that is decrypted by signing a payload with Windows Hello.
        /// So if an malicious actor gets access to the Windows Credential Manager, they can not access the main encryption key.
        /// </summary>
        private static PasswordVault? passwordVault;

        private static void InitPlatform()
        {
            passwordVault = new();
        }

        private static Task RegisterQuickUnlock()
        {
            return RegisterWindowsHello();
        }

        internal static void DeletePlatformCredentials()
        {
            if (IsWindowsHelloRegistered())
            {
                _ = WindowsHello.Unregister();
                DeleteWindowsHelloProtectedKey();
            }
        }

        public static async Task RegisterWindowsHello()
        {
            if (authData == null || mainEncryptionKey == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (IsWindowsHelloRegistered())
            {
                throw new Exception("Windows Hello already registered");
            }
            authData.WindowsHelloChallenge ??= EncryptionHelper.GetRandomBase64String(128);
            var windowsHelloResult = await WindowsHello.Register();
            if (
                windowsHelloResult.Status
                    != Windows.Security.Credentials.KeyCredentialStatus.Success
                && windowsHelloResult.Status
                    != Windows.Security.Credentials.KeyCredentialStatus.CredentialAlreadyExists
            )
            {
                throw new Exception(
                    $"Failed to register Windows Hello: {windowsHelloResult.Status}"
                );
            }

            // If registration failed before this can happen
            if (
                windowsHelloResult.Status
                == Windows.Security.Credentials.KeyCredentialStatus.CredentialAlreadyExists
            )
            {
                await WindowsHello.Unregister();
                windowsHelloResult = await WindowsHello.Register();
                if (
                    windowsHelloResult.Status
                    != Windows.Security.Credentials.KeyCredentialStatus.Success
                )
                {
                    throw new Exception(
                        $"Failed to register Windows Hello: {windowsHelloResult.Status}"
                    );
                }
            }

            string? signedChallenge = await WindowsHello.SignChallenge(
                windowsHelloResult.Credential,
                authData.WindowsHelloChallenge
            );
            if (signedChallenge == null || signedChallenge.Length == 0)
            {
                throw new Exception(
                    "Failed to register with Windows Hello because the signed challenge is empty"
                );
            }
            EncryptionHelper encryptionHelper = new(
                Encoding.UTF8.GetBytes(signedChallenge),
                Convert.FromBase64String(authData.LoginSalt)
            );
            SetWindowsHelloProtectedKey(encryptionHelper.EncryptString(mainEncryptionKey));
        }

        public static bool IsWindowsHelloRegistered()
        {
            try
            {
                _ = GetWindowsHelloProtectedKey();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetWindowsHelloProtectedKey()
        {
            ArgumentNullException.ThrowIfNull(authData);
            ArgumentNullException.ThrowIfNull(passwordVault);

            PasswordCredential cred = passwordVault.Retrieve("2FAGuard", authData.InstallationID);
            cred.RetrievePassword();
            return cred.Password;
        }

        public static void SetWindowsHelloProtectedKey(string key)
        {
            ArgumentNullException.ThrowIfNull(authData);
            ArgumentNullException.ThrowIfNull(passwordVault);

            PasswordCredential cred = new("2FAGuard", authData.InstallationID, key);
            passwordVault.Add(cred);
        }

        public static void DeleteWindowsHelloProtectedKey()
        {
            ArgumentNullException.ThrowIfNull(authData);
            ArgumentNullException.ThrowIfNull(passwordVault);

            PasswordCredential cred = passwordVault.Retrieve("2FAGuard", authData.InstallationID);
            passwordVault.Remove(cred);
        }

        public static async Task LoginWithWindowsHello()
        {
            if (authData == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            string windowsHelloProtectedKey =
                GetWindowsHelloProtectedKey() ?? throw new Exception("Windows Hello not enabled");

            string signedChallenge = await WindowsHello.GetSignedChallenge();
            if (signedChallenge == null || signedChallenge.Length == 0)
            {
                throw new Exception(
                    "Failed to login with Windows Hello because the signed challenge is empty"
                );
            }
            EncryptionHelper encryptionHelper = new(
                Encoding.UTF8.GetBytes(signedChallenge),
                Convert.FromBase64String(authData.LoginSalt)
            );
            mainEncryptionKey = encryptionHelper.DecryptString(windowsHelloProtectedKey);

            if (mainEncryptionKey == null)
            {
                throw new Exception("Failed to decrypt encryption keys");
            }
        }

        public static string GetWindowsHelloChallenge()
        {
            if (authData == null || authData.WindowsHelloChallenge == null)
            {
                throw new Exception("Windows Hello challenge not set");
            }
            return authData.WindowsHelloChallenge;
        }

        public static async void UnregisterWindowsHello()
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (!IsWindowsHelloRegistered())
            {
                throw new Exception("Windows Hello not enabled");
            }
            DeleteWindowsHelloProtectedKey();
            await WindowsHello.Unregister();
        }

        public static async Task LoginWithWebAuthn(IntPtr windowHandle)
        {
            if (authData == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }

            var result = await WebAuthnHelper.Assert(windowHandle, GetWebAuthnDevices());

            if (!result.success)
            {
                throw new Exception("WebAuthn assertion failed: " + result.error);
            }

            if (result.result == null)
            {
                throw new Exception("Assertion result is null");
            }

            UnlockWithAssertion(result.result);
        }
    }
}
