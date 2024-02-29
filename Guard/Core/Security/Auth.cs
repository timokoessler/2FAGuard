using System.Drawing.Drawing2D;
using System.Text.Json;
using System.Windows;
using Guard.Core.Models;
using Guard.Core.Storage;
using Microsoft.Win32;

namespace Guard.Core.Security
{
    internal class Auth
    {
        private static readonly string authFilePath = System.IO.Path.Combine(
            Utils.GetAppDataFolderPath(),
            "auth-keys"
        );
        private static AuthFileData? authData;
        private static string? mainEncryptionKey;
        private static EncryptionHelper? mainEncryptionHelper;
        private static readonly int currentVersion = 1;
        private static MainWindow? mainWindow;

        public static async Task Init()
        {
            if (authData != null)
            {
                // Already initialized
                return;
            }
            if (FileExists())
            {
                await LoadFile();
            }
            else
            {
                if (!System.IO.Directory.Exists(Utils.GetAppDataFolderPath()))
                {
                    System.IO.Directory.CreateDirectory(Utils.GetAppDataFolderPath());
                }
                authData = new AuthFileData();
            }
            mainWindow = (MainWindow)Application.Current.MainWindow;
            SystemEvents.SessionSwitch += OnSessionSwitch;
        }

        public static bool FileExists()
        {
            return System.IO.File.Exists(authFilePath);
        }

        public static async Task LoadFile()
        {
            byte[] fileData = await System.IO.File.ReadAllBytesAsync(authFilePath);
            string fileContent = System.Text.Encoding.UTF8.GetString(fileData);
            authData = JsonSerializer.Deserialize<AuthFileData>(fileContent);
        }

        public static async Task SaveFile()
        {
            string fileContent = JsonSerializer.Serialize(authData);
            byte[] fileData = System.Text.Encoding.UTF8.GetBytes(fileContent);
            await System.IO.File.WriteAllBytesAsync(authFilePath, fileData);
        }

        /// <summary>
        /// Create a new encryption key and encrypt it with the password
        /// Also generate a salt
        /// </summary>
        /// <param name="password">The user chosen password to encrypt the key with</param>
        /// <param name="enableWindowsHello">If Windows Hello should be enabled</param>
        public static async Task Register(string password, bool enableWindowsHello)
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (
                authData.PasswordProtectedKey != null
                || authData.WindowsHelloProtectedKey != null
                || authData.InsecureMainKey != null
                || mainEncryptionKey != null
            )
            {
                throw new Exception("Already registered");
            }
            mainEncryptionKey = EncryptionHelper.GetRandomBase64String(128);
            authData.KeySalt = EncryptionHelper.GenerateSalt();

            string loginSalt = EncryptionHelper.GenerateSalt();
            authData.LoginSalt = loginSalt;

            EncryptionHelper encryptionHelper = new(password, loginSalt);

            authData.PasswordProtectedKey = encryptionHelper.EncryptString(mainEncryptionKey);
            authData.Version = currentVersion;

            if (enableWindowsHello)
            {
                await RegisterWindowsHello();
            }
            await SaveFile();
        }

        public static async Task RegisterWindowsHello()
        {
            if (authData == null || mainEncryptionKey == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (authData.WindowsHelloProtectedKey != null)
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
            EncryptionHelper encryptionHelper = new(signedChallenge, authData.LoginSalt);
            authData.WindowsHelloProtectedKey = encryptionHelper.EncryptString(mainEncryptionKey);
        }

        public static async Task RegisterInsecure()
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (
                authData.PasswordProtectedKey != null
                || authData.WindowsHelloProtectedKey != null
                || mainEncryptionKey != null
            )
            {
                throw new Exception("Already registered");
            }
            mainEncryptionKey = EncryptionHelper.GetRandomBase64String(128);

            authData.LoginSalt = EncryptionHelper.GenerateSalt();
            authData.InsecureMainKey = mainEncryptionKey;
            authData.KeySalt = EncryptionHelper.GenerateSalt();
            authData.Version = currentVersion;
            await SaveFile();
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
            return authData?.WindowsHelloProtectedKey != null;
        }

        public static async Task LoginWithWindowsHello()
        {
            if (authData == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (authData.WindowsHelloProtectedKey == null)
            {
                throw new Exception("Windows Hello not registered");
            }
            string signedChallenge = await WindowsHello.GetSignedChallenge();
            if (signedChallenge == null || signedChallenge.Length == 0)
            {
                throw new Exception(
                    "Failed to login with Windows Hello because the signed challenge is empty"
                );
            }
            EncryptionHelper encryptionHelper = new(signedChallenge, authData.LoginSalt);
            mainEncryptionKey = encryptionHelper.DecryptString(authData.WindowsHelloProtectedKey);

            if (mainEncryptionKey == null)
            {
                throw new Exception("Failed to decrypt encryption keys");
            }
        }

        public static void LoginWithPassword(string password)
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
                EncryptionHelper encryptionHelper = new(password, authData.LoginSalt);
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
            mainEncryptionHelper ??= new EncryptionHelper(mainEncryptionKey, authData.KeySalt);
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
            TokenManager.ClearTokens();
        }

        public static async void UnregisterWindowsHello()
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (authData.WindowsHelloProtectedKey == null)
            {
                throw new Exception("Windows Hello not enabled");
            }
            authData.WindowsHelloProtectedKey = null;
            authData.WindowsHelloChallenge = null;
            await WindowsHello.Unregister();
        }

        public static bool CheckPassword(string password)
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
                EncryptionHelper encryptionHelper = new(password, authData.LoginSalt);
                _ = encryptionHelper.DecryptString(authData.PasswordProtectedKey) ?? throw new Exception("Invalid password");
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static async Task ChangePassword(string newPassword)
        {
            if (authData == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if(mainEncryptionKey == null)
            {
                throw new Exception("Main encryption key not set");
            }
            EncryptionHelper encryptionHelper = new(newPassword, authData.LoginSalt);
            authData.PasswordProtectedKey = encryptionHelper.EncryptString(mainEncryptionKey);
            authData.InsecureMainKey = null;
            await SaveFile();
        }

        private static void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock && mainEncryptionKey != null && SettingsManager.Settings.LockOnScreenLock)
            {
                mainWindow?.Logout();
            }
        }
    }
}
