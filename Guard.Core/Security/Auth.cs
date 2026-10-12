using System.Text;
using System.Text.Json;
using Guard.Core.Models;
using Guard.Core.Security.WebAuthn;
using Guard.Core.Storage;

namespace Guard.Core.Security
{
    public partial class Auth
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
                    Version = currentVersion,
                };
            }
            InitPlatform();
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
            await SafeFileWriter.SaveFileAsync(authFilePath, fileData);
        }

        /// <summary>
        /// Create a new encryption key and encrypt it with the password
        /// Also generate a salt
        /// </summary>
        /// <param name="password">The user chosen password to encrypt the key with</param>
        /// <param name="enableWindowsHello">If Windows Hello (on Linux: the system keyring) should be enabled</param>
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

            EncryptionHelper encryptionHelper = new(
                password,
                Convert.FromBase64String(authData.LoginSalt)
            );
            authData.PasswordProtectedKey = encryptionHelper.EncryptString(mainEncryptionKey);

            if (enableWindowsHello)
            {
                await RegisterQuickUnlock();
            }
            await SaveFile();
            registrationFinished = true;
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
                EncryptionHelper encryptionHelper = new(
                    password,
                    Convert.FromBase64String(authData.LoginSalt)
                );
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

        public static void Logout()
        {
            mainEncryptionKey = null;
            mainEncryptionHelper = null;
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
                EncryptionHelper encryptionHelper = new(
                    password,
                    Convert.FromBase64String(authData.LoginSalt)
                );
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
            EncryptionHelper encryptionHelper = new(
                newPassword,
                Convert.FromBase64String(authData.LoginSalt)
            );
            authData.PasswordProtectedKey = encryptionHelper.EncryptString(mainEncryptionKey);
            authData.InsecureMainKey = null;
            await SaveFile();
        }

        public static string GetInstallationID()
        {
            ArgumentNullException.ThrowIfNull(authData);
            return authData.InstallationID;
        }

        public static List<WebauthnDevice> GetWebAuthnDevices()
        {
            ArgumentNullException.ThrowIfNull(authData);
            return authData.WebAuthn ?? [];
        }

        internal static async Task AddWebAuthnDevice(WebauthnDevice device, AssertionResult result)
        {
            ArgumentNullException.ThrowIfNull(authData);
            ArgumentNullException.ThrowIfNull(authData.LoginSalt);
            ArgumentNullException.ThrowIfNull(mainEncryptionKey);

            authData.WebAuthn ??= [];

            EncryptionHelper encryptionHelper = new(
                result.HmacSecret,
                Convert.FromBase64String(authData.LoginSalt)
            );

            device.ProtectedKey = encryptionHelper.EncryptString(mainEncryptionKey);

            authData.WebAuthn.Add(device);
            await SaveFile();
        }

        public static async Task RemoveWebAuthnDevice(WebauthnDevice device)
        {
            ArgumentNullException.ThrowIfNull(authData);

            authData.WebAuthn ??= [];

            authData.WebAuthn.Remove(device);
            await SaveFile();
        }

        private static void UnlockWithAssertion(AssertionResult assertion)
        {
            ArgumentNullException.ThrowIfNull(authData);
            ArgumentNullException.ThrowIfNull(authData.LoginSalt);

            var device =
                (
                    authData.WebAuthn?.Find(d =>
                        d.Id == Convert.ToBase64String(assertion.CredentialId)
                    )
                ) ?? throw new Exception("Can not find webauthn device in auth file");

            EncryptionHelper encryptionHelper = new(
                assertion.HmacSecret,
                Convert.FromBase64String(authData.LoginSalt)
            );
            mainEncryptionKey = encryptionHelper.DecryptString(
                device.ProtectedKey ?? throw new Exception("Protected key not set")
            );

            if (mainEncryptionKey == null)
            {
                throw new Exception("Failed to decrypt encryption keys");
            }
        }
    }
}
