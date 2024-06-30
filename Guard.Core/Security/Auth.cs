using System.Text;
using System.Text.Json;
using Guard.Core.Models;
using Windows.Security.Credentials;

namespace Guard.Core.Security
{
    public class Auth
    {
        private static readonly string authFilePath = Path.Combine(
            InstallationContext.GetAppDataFolderPath(),
            "auth-keys"
        );
        private static AuthFileData? authData;
        private static string? mainEncryptionKey;
        private static EncryptionHelper? mainEncryptionHelper;
        private static readonly int currentVersion = 1;
        private static bool registrationFinished = false;

        /// <summary>
        /// The password vault is used to store a encrypted version of the main encryption key that is decrypted by signing a payload with Windows Hello.
        /// So if an malicious actor gets access to the Windows Credential Manager, they can not access the main encryption key.
        /// </summary>
        private static PasswordVault? passwordVault;

        public static async Task Init()
        {
            if (authData != null)
            {
                // Already initialized
                return;
            }
            if (FileExists())
            {
                registrationFinished = true;
                await LoadFile();
            }
            else
            {
                if (!Directory.Exists(InstallationContext.GetAppDataFolderPath()))
                {
                    Directory.CreateDirectory(InstallationContext.GetAppDataFolderPath());
                }
                authData = new AuthFileData
                {
                    InstallationID = EncryptionHelper.GetRandomHexString(18),
                    Version = currentVersion
                };
            }
            passwordVault = new();
        }

        public static bool FileExists()
        {
            return File.Exists(authFilePath);
        }

        public static async Task LoadFile()
        {
            byte[] fileData = await File.ReadAllBytesAsync(authFilePath);
            string fileContent = Encoding.UTF8.GetString(fileData);
            authData = JsonSerializer.Deserialize<AuthFileData>(fileContent);
        }

        public static async Task SaveFile()
        {
            byte[] fileData = JsonSerializer.SerializeToUtf8Bytes(authData);
            await File.WriteAllBytesAsync(authFilePath, fileData);
        }

        /// <summary>
        /// Create a new encryption key and encrypt it with the password
        /// Also generate a salt
        /// </summary>
        /// <param name="password">The user chosen password to encrypt the key with</param>
        /// <param name="enableWindowsHello">If Windows Hello should be enabled</param>
        public static async Task Register(byte[] password, bool enableWindowsHello)
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (registrationFinished)
            {
                throw new Exception("Already registered");
            }
            mainEncryptionKey = EncryptionHelper.GetRandomBase64String(128);
            authData.KeySalt = EncryptionHelper.GenerateSalt();
            authData.LoginSalt = EncryptionHelper.GenerateSalt();

            EncryptionHelper encryptionHelper =
                new(password, Convert.FromBase64String(authData.LoginSalt));
            authData.PasswordProtectedKey = encryptionHelper.EncryptString(mainEncryptionKey);

            if (enableWindowsHello)
            {
                await RegisterWindowsHello();
            }
            await SaveFile();
            registrationFinished = true;
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
            authData.WindowsHelloChallenge = EncryptionHelper.GetRandomBase64String(128);
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
            EncryptionHelper encryptionHelper =
                new(
                    Encoding.UTF8.GetBytes(signedChallenge),
                    Convert.FromBase64String(authData.LoginSalt)
                );
            SetWindowsHelloProtectedKey(encryptionHelper.EncryptString(mainEncryptionKey));
        }

        public static async Task RegisterInsecure()
        {
            ArgumentNullException.ThrowIfNull(authData);
            if (registrationFinished)
            {
                throw new Exception("Already registered");
            }
            mainEncryptionKey = EncryptionHelper.GetRandomBase64String(128);

            authData.LoginSalt = EncryptionHelper.GenerateSalt();
            authData.InsecureMainKey = mainEncryptionKey;
            authData.KeySalt = EncryptionHelper.GenerateSalt();
            authData.Version = currentVersion;
            await SaveFile();
            registrationFinished = true;
        }

        public static bool IsLoggedIn()
        {
            return mainEncryptionKey != null;
        }

        public static bool IsLoginEnabled()
        {
            return authData?.InsecureMainKey == null;
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
            EncryptionHelper encryptionHelper =
                new(
                    Encoding.UTF8.GetBytes(signedChallenge),
                    Convert.FromBase64String(authData.LoginSalt)
                );
            mainEncryptionKey = encryptionHelper.DecryptString(windowsHelloProtectedKey);

            if (mainEncryptionKey == null)
            {
                throw new Exception("Failed to decrypt encryption keys");
            }
        }

        public static void LoginWithPassword(byte[] password)
        {
            if (authData == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (authData.PasswordProtectedKey == null)
            {
                throw new Exception("Password not set");
            }
            try
            {
                EncryptionHelper encryptionHelper =
                    new(password, Convert.FromBase64String(authData.LoginSalt));
                mainEncryptionKey = encryptionHelper.DecryptString(authData.PasswordProtectedKey);
            }
            catch
            {
                throw new Exception("Failed to decrypt keys");
            }
            if (mainEncryptionKey == null)
            {
                throw new Exception("Failed to decrypt keys");
            }
        }

        public static void LoginInsecure()
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (authData.InsecureMainKey == null)
            {
                throw new Exception("Insecure main key not set");
            }
            mainEncryptionKey = authData.InsecureMainKey;
        }

        public static EncryptionHelper GetMainEncryptionHelper()
        {
            if (mainEncryptionKey == null)
            {
                throw new Exception("Main encryption key not set");
            }
            if (authData == null || authData.KeySalt == null)
            {
                throw new Exception("Key salt not set");
            }
            mainEncryptionHelper ??= new EncryptionHelper(
                Encoding.UTF8.GetBytes(mainEncryptionKey),
                Convert.FromBase64String(authData.KeySalt)
            );
            return mainEncryptionHelper;
        }

        public static string GetWindowsHelloChallenge()
        {
            if (authData == null || authData.WindowsHelloChallenge == null)
            {
                throw new Exception("Windows Hello challenge not set");
            }
            return authData.WindowsHelloChallenge;
        }

        public static void Logout()
        {
            mainEncryptionKey = null;
            mainEncryptionHelper = null;
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

        public static bool CheckPassword(byte[] password)
        {
            if (authData == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (authData.PasswordProtectedKey == null)
            {
                throw new Exception("Password not set");
            }
            try
            {
                EncryptionHelper encryptionHelper =
                    new(password, Convert.FromBase64String(authData.LoginSalt));
                _ =
                    encryptionHelper.DecryptString(authData.PasswordProtectedKey)
                    ?? throw new Exception("Invalid password");
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static async Task ChangePassword(byte[] newPassword)
        {
            if (authData == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (mainEncryptionKey == null)
            {
                throw new Exception("Main encryption key not set");
            }
            EncryptionHelper encryptionHelper =
                new(newPassword, Convert.FromBase64String(authData.LoginSalt));
            authData.PasswordProtectedKey = encryptionHelper.EncryptString(mainEncryptionKey);
            authData.InsecureMainKey = null;
            await SaveFile();
        }

        public static string GetInstallationID()
        {
            ArgumentNullException.ThrowIfNull(authData);
            return authData.InstallationID;
        }
    }
}
