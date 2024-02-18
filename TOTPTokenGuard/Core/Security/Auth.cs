using System.Text.Json;
using TOTPTokenGuard.Core.Models;

namespace TOTPTokenGuard.Core.Security
{
    internal class Auth
    {
        private static readonly string authFilePath = System.IO.Path.Combine(
            Utils.GetAppDataFolderPath(),
            "auth-keys.bin"
        );
        public static AuthFileData? authData;
        public static string? mainEncryptionKey;
        public static string? dbEncryptionKey;

        public static void Init()
        {
            if (FileExists())
            {
                LoadFile();
            }
            else
            {
                authData = new AuthFileData();
            }
        }

        public static bool FileExists()
        {
            return System.IO.File.Exists(authFilePath);
        }

        public static void LoadFile()
        {
            byte[] fileData = System.IO.File.ReadAllBytes(authFilePath);
            string fileContent = System.Text.Encoding.UTF8.GetString(fileData);
            authData = JsonSerializer.Deserialize<AuthFileData>(fileContent);
        }

        public static void SaveFile()
        {
            string fileContent = JsonSerializer.Serialize(authData);
            byte[] fileData = System.Text.Encoding.UTF8.GetBytes(fileContent);
            System.IO.File.WriteAllBytes(authFilePath, fileData);
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
            dbEncryptionKey = EncryptionHelper.GetRandomBase64String(64);

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
            SaveFile();
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
            )
            {
                throw new Exception(
                    $"Failed to register Windows Hello: {windowsHelloResult.Status}"
                );
            }
            string? signedChallenge = await WindowsHello.SignChallenge(
                windowsHelloResult.Credential,
                authData.WindowsHelloChallenge
            );
            if (signedChallenge == null || signedChallenge.Length == 0)
            {
                throw new Exception(
                    "Failed to sign Windows Hello challenge because the signed challenge is empty"
                );
            }
            authData.WindowsHelloProtectedKey = EncryptionHelper.EncryptString(
                signedChallenge,
                mainEncryptionKey
            );
        }

        public static bool IsLoggedIn()
        {
            return mainEncryptionKey != null && dbEncryptionKey != null;
        }
    }
}
