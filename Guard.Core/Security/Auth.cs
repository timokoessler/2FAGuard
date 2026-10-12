using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Guard.Core.Models;
using Guard.Core.Security.WebAuthn;
using Guard.Core.Storage;
using Guard.Core.Platform;

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
        
        private static ISecureStorage? _secureStorage;

        public static async Task Init()
        {
            if (authData != null)
            {
                return;
            }
            
            _secureStorage = PlatformService.Instance.GetSecureStorage();
            
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

        public static async Task Register(byte[] password, bool enableBiometrics)
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

            if (enableBiometrics)
            {
                await RegisterBiometrics();
            }
            await SaveFile();
            registrationFinished = true;
        }

        public static async Task RegisterBiometrics()
        {
            if (authData == null || mainEncryptionKey == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (IsBiometricsRegistered())
            {
                throw new Exception("Biometrics already registered");
            }
            authData.BiometricChallenge ??= EncryptionHelper.GetRandomBase64String(128);
            
            var registered = await BiometricAuth.RegisterAsync();
            if (!registered)
            {
                throw new Exception("Failed to register biometrics");
            }

            string? signedChallenge = await BiometricAuth.GetSignedChallengeAsync();
            if (string.IsNullOrEmpty(signedChallenge))
            {
                throw new Exception("Failed to register with biometrics because the signed challenge is empty");
            }
            
            var encryptionHelper = new EncryptionHelper(
                Encoding.UTF8.GetBytes(signedChallenge),
                Convert.FromBase64String(authData.LoginSalt)
            );
            SetBiometricProtectedKey(encryptionHelper.EncryptString(mainEncryptionKey));
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

        public static bool IsBiometricsRegistered()
        {
            try
            {
                _ = GetBiometricProtectedKey();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetBiometricProtectedKey()
        {
            ArgumentNullException.ThrowIfNull(authData);
            ArgumentNullException.ThrowIfNull(_secureStorage);

            return _secureStorage.Get("2FAGuard", authData.InstallationID) 
                ?? throw new Exception("Biometrics not enabled");
        }

        public static void SetBiometricProtectedKey(string key)
        {
            ArgumentNullException.ThrowIfNull(authData);
            ArgumentNullException.ThrowIfNull(_secureStorage);

            _secureStorage.Set("2FAGuard", authData.InstallationID, key);
        }

        public static void DeleteBiometricProtectedKey()
        {
            ArgumentNullException.ThrowIfNull(authData);
            ArgumentNullException.ThrowIfNull(_secureStorage);

            _secureStorage.Delete("2FAGuard", authData.InstallationID);
        }

        public static async Task LoginWithBiometrics()
        {
            if (authData == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            string biometricProtectedKey =
                GetBiometricProtectedKey() ?? throw new Exception("Biometrics not enabled");

            string signedChallenge = await BiometricAuth.GetSignedChallengeAsync() 
                ?? throw new Exception("Failed to login with biometrics because the signed challenge is empty");
            
            if (signedChallenge.Length == 0)
            {
                throw new Exception("Failed to login with biometrics because the signed challenge is empty");
            }
            
            var encryptionHelper = new EncryptionHelper(
                Encoding.UTF8.GetBytes(signedChallenge),
                Convert.FromBase64String(authData.LoginSalt)
            );
            mainEncryptionKey = encryptionHelper.DecryptString(biometricProtectedKey);

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
                var encryptionHelper = new EncryptionHelper(
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

        public static string GetBiometricChallenge()
        {
            if (authData == null || authData.BiometricChallenge == null)
            {
                throw new Exception("Biometric challenge not set");
            }
            return authData.BiometricChallenge;
        }

        public static void Logout()
        {
            mainEncryptionKey = null;
            mainEncryptionHelper = null;
        }

        public static async Task UnregisterBiometrics()
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (!IsBiometricsRegistered())
            {
                throw new Exception("Biometrics not enabled");
            }
            DeleteBiometricProtectedKey();
            await BiometricAuth.UnregisterAsync();
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
                var encryptionHelper = new EncryptionHelper(
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
            var encryptionHelper = new EncryptionHelper(
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

            var encryptionHelper = new EncryptionHelper(
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

            var device =
                (
                    authData.WebAuthn?.Find(d =>
                        d.Id == Convert.ToBase64String(result.result.CredentialId)
                    )
                ) ?? throw new Exception("Can not find webauthn device in auth file");

            var encryptionHelper = new EncryptionHelper(
                result.result.HmacSecret,
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