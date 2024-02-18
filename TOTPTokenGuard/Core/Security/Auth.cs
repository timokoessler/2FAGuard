using System.Text.Json;
using TOTPTokenGuard.Core.Models;

namespace TOTPTokenGuard.Core.Security
{
    internal class Auth
    {
        private static readonly string authFilePath = System.IO.Path.Combine(
            Utils.GetAppDataFolderPath(),
            "auth-keys"
        );
        public static AuthFileData? authData;
        public static string? mainEncryptionKey;
        public static string? dbEncryptionKey;

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

        public static async Task Register(string password, bool enableWindowsHello)
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (
                authData.PasswordProtectedKey != null
                || authData.ProtectedDbKey != null
                || mainEncryptionKey != null
            )
            {
                throw new Exception("Already registered");
            }
            mainEncryptionKey = EncryptionHelper.GetRandomBase64String(128);
            dbEncryptionKey = EncryptionHelper.GetRandomBase64String(128);

            authData.PasswordProtectedKey = EncryptionHelper.EncryptString(
                mainEncryptionKey,
                password
            );
            authData.ProtectedDbKey = EncryptionHelper.EncryptString(
                dbEncryptionKey,
                mainEncryptionKey
            );

            if (enableWindowsHello)
            {
                await RegisterWindowsHello();
            }
            await SaveFile();
        }

        private static async Task RegisterWindowsHello()
        {
            if (authData == null || mainEncryptionKey == null)
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
            authData.WindowsHelloProtectedKey = EncryptionHelper.EncryptString(
                mainEncryptionKey,
                signedChallenge
            );
        }

        public static bool IsLoggedIn()
        {
            return mainEncryptionKey != null && dbEncryptionKey != null;
        }

        public static bool isLoginEnabled()
        {
            return authData?.InsecureMainKey == null;
        }

        public static bool IsWindowsHelloRegistered()
        {
            return authData?.WindowsHelloProtectedKey != null;
        }

        public static async Task LoginWithWindowsHello()
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (authData.WindowsHelloProtectedKey == null)
            {
                throw new Exception("Windows Hello not registered");
            }
            if (authData.ProtectedDbKey == null)
            {
                throw new Exception("Emergency: DB encryption key is not set");
            }
            string signedChallenge = await WindowsHello.GetSignedChallenge();
            if (signedChallenge == null || signedChallenge.Length == 0)
            {
                throw new Exception(
                    "Failed to login with Windows Hello because the signed challenge is empty"
                );
            }
            mainEncryptionKey = EncryptionHelper.DecryptString(
                authData.WindowsHelloProtectedKey,
                signedChallenge
            );
            dbEncryptionKey = EncryptionHelper.DecryptString(
                authData.ProtectedDbKey,
                mainEncryptionKey
            );
            if (mainEncryptionKey == null || dbEncryptionKey == null)
            {
                throw new Exception("Failed to decrypt encryption keys");
            }
        }

        public static void LoginWithPassword(string password)
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (authData.PasswordProtectedKey == null)
            {
                throw new Exception("Password not set");
            }
            if (authData.ProtectedDbKey == null)
            {
                throw new Exception("Emergency: DB encryption key is not set");
            }
            try
            {
                mainEncryptionKey = EncryptionHelper.DecryptString(
                    authData.PasswordProtectedKey,
                    password
                );
                dbEncryptionKey = EncryptionHelper.DecryptString(
                    authData.ProtectedDbKey,
                    mainEncryptionKey
                );
            }
            catch
            {
                throw new Exception("Failed to decrypt keys");
            }
            if (mainEncryptionKey == null || dbEncryptionKey == null)
            {
                throw new Exception("Failed to decrypt keys");
            }
        }

        public static async Task RegisterInsecure()
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (
                authData.PasswordProtectedKey != null
                || authData.ProtectedDbKey != null
                || mainEncryptionKey != null
            )
            {
                throw new Exception("Already registered");
            }
            mainEncryptionKey = EncryptionHelper.GetRandomBase64String(128);
            dbEncryptionKey = EncryptionHelper.GetRandomBase64String(128);
            authData.ProtectedDbKey = EncryptionHelper.EncryptString(
                dbEncryptionKey,
                mainEncryptionKey
            );

            authData.InsecureMainKey = mainEncryptionKey;
            await SaveFile();
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
            if (authData.ProtectedDbKey == null)
            {
                throw new Exception("Emergency: DB encryption key is not set");
            }
            try
            {
                mainEncryptionKey = authData.InsecureMainKey;
                dbEncryptionKey = EncryptionHelper.DecryptString(
                    authData.ProtectedDbKey,
                    mainEncryptionKey
                );
            }
            catch
            {
                throw new Exception("Failed to decrypt keys");
            }
            if (mainEncryptionKey == null || dbEncryptionKey == null)
            {
                throw new Exception("Failed to decrypt keys");
            }
        }
    }
}
