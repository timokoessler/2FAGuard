using System.Text;
using Guard.Core.Models;
using Guard.Core.Security.Linux;
using Guard.Core.Security.WebAuthn;

namespace Guard.Core.Security
{
    public partial class Auth
    {
        private const string SecurityKeyRpId = "2faguard.app";
        private const string SecurityKeyRpName = "2FAGuard";
        private const string SecurityKeyUserName = "2FAGuard User";

        private static void InitPlatform() { }

        private static Task RegisterQuickUnlock()
        {
            return RegisterKeyringUnlock();
        }

        internal static void DeletePlatformCredentials()
        {
            if (authData == null)
            {
                return;
            }
            try
            {
                SecretServiceKeyring.Clear(authData.InstallationID);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Failed to remove keyring secret: {0}", ex.Message);
            }
        }

        public static bool IsKeyringUnlockAvailable()
        {
            return SecretServiceKeyring.IsAvailable();
        }

        public static bool IsKeyringUnlockRegistered()
        {
            return authData?.KeyringProtectedKey != null;
        }

        /// <summary>
        /// The main key is encrypted with a random secret that is stored in the Secret Service.
        /// Both the auth file and the keyring entry are needed to unlock the app.
        /// </summary>
        public static async Task RegisterKeyringUnlock()
        {
            if (authData == null || mainEncryptionKey == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            if (IsKeyringUnlockRegistered())
            {
                throw new Exception("System keyring unlock already registered");
            }
            string secret = EncryptionHelper.GetRandomBase64String(64);
            string label = InstallationContext.IsPortable() ? "2FAGuard (Portable)" : "2FAGuard";
            await Task.Run(() => SecretServiceKeyring.Store(authData.InstallationID, label, secret));

            EncryptionHelper encryptionHelper = new(
                Encoding.UTF8.GetBytes(secret),
                Convert.FromBase64String(authData.LoginSalt)
            );
            authData.KeyringProtectedKey = encryptionHelper.EncryptString(mainEncryptionKey);
        }

        public static async Task LoginWithKeyring()
        {
            if (authData == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            string protectedKey =
                authData.KeyringProtectedKey
                ?? throw new Exception("System keyring unlock not enabled");

            if (await PolkitVerification.VerifyAsync() == PolkitVerification.Result.Canceled)
            {
                throw new OperationCanceledException("UserCanceled");
            }

            string secret =
                await Task.Run(() => SecretServiceKeyring.Lookup(authData.InstallationID))
                ?? throw new Exception(
                    "The secret was not found in the system keyring. Please log in with your password and enable the keyring unlock again."
                );

            EncryptionHelper encryptionHelper = new(
                Encoding.UTF8.GetBytes(secret),
                Convert.FromBase64String(authData.LoginSalt)
            );
            mainEncryptionKey = encryptionHelper.DecryptString(protectedKey);

            if (mainEncryptionKey == null)
            {
                throw new Exception("Failed to decrypt encryption keys");
            }
        }

        public static void UnregisterKeyringUnlock()
        {
            if (authData == null)
            {
                throw new Exception("Auth data not initialized");
            }
            authData.KeyringProtectedKey = null;
            SecretServiceKeyring.Clear(authData.InstallationID);
        }

        public static async Task LoginWithSecurityKey(Fido2DeviceInfo device, string? pin)
        {
            if (authData == null || authData.LoginSalt == null)
            {
                throw new Exception("Auth data not initialized");
            }
            AssertionResult result = await Task.Run(() => AssertSecurityKey(device, pin, GetWebAuthnDevices()));
            UnlockWithAssertion(result);
        }

        public static async Task RegisterSecurityKey(Fido2DeviceInfo device, string? pin, string keyName)
        {
            ArgumentNullException.ThrowIfNull(authData);

            await Task.Run(async () =>
            {
                List<byte[]> exclude = GetWebAuthnDevices()
                    .Where(d => d.Id != null)
                    .Select(d => Convert.FromBase64String(d.Id!))
                    .ToList();

                byte[] credentialId = Fido2.MakeCredential(
                    device,
                    pin,
                    SecurityKeyRpId,
                    SecurityKeyRpName,
                    Encoding.UTF8.GetBytes(authData.InstallationID),
                    SecurityKeyUserName,
                    exclude
                );

                WebauthnDevice webauthnDevice = new()
                {
                    Id = Convert.ToBase64String(credentialId),
                    EncryptedName = GetMainEncryptionHelper().EncryptString(keyName),
                    ProtectedKey = "",
                    Salt1 = EncryptionHelper.GetRandomBase64String(32),
                    Salt2 = EncryptionHelper.GetRandomBase64String(32),
                };

                AssertionResult assertion = AssertSecurityKey(device, pin, [webauthnDevice]);
                if (!assertion.CredentialId.SequenceEqual(credentialId))
                {
                    throw new Exception("CredentialId does not match");
                }

                await AddWebAuthnDevice(webauthnDevice, assertion);
            });
        }

        private static AssertionResult AssertSecurityKey(
            Fido2DeviceInfo device,
            string? pin,
            List<WebauthnDevice> devices
        )
        {
            Dictionary<byte[], (byte[], byte[])> salts = [];
            foreach (WebauthnDevice d in devices)
            {
                if (d.Id == null || d.Salt1 == null || d.Salt2 == null)
                {
                    continue;
                }
                salts[Convert.FromBase64String(d.Id)] = (
                    Convert.FromBase64String(d.Salt1),
                    Convert.FromBase64String(d.Salt2)
                );
            }
            if (salts.Count == 0)
            {
                throw new Exception("No security keys are registered");
            }

            var (credentialId, hmacSecret) = Fido2.GetHmacSecret(device, pin, SecurityKeyRpId, salts);
            if (!salts.Keys.Any(id => id.SequenceEqual(credentialId)))
            {
                throw new Exception("CredentialId is not in allowedCredentials");
            }
            return new AssertionResult { CredentialId = credentialId, HmacSecret = hmacSecret };
        }
    }
}
