using System.IO;
using System.Text;
using System.Text.Json;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.WPF.Core.Icons;
using Guard.WPF.Core.Models;
using NSec.Cryptography;

namespace Guard.WPF.Core.Import.Importer
{
    internal class AuthenticatorProImporter : IImporter
    {
        public string Name => "AuthenticatorPro";
        public IImporter.ImportType Type => IImporter.ImportType.File;
        public string SupportedFileExtensions =>
            "Stratum / Authenticator Pro Backup (*.stratum, *.authpro) | *.stratum; *.authpro";

        private enum BackupType
        {
            NoEncryption,
            LegacyEncryption,
            StrongEncryption,
            Invalid,
        }

        private const string LegacyHeader = "AuthenticatorPro";
        private const string StrongHeader = "AUTHENTICATORPRO";

        private const int ArgonParallelism = 4;
        private const int ArgonIterations = 3;
        private const int ArgonMemorySize = 65536;

        private const int SaltLength = 16;
        private const int KeyLength = 32;
        private const int IvLength = 12;

        public (int total, int duplicate, int tokenID) Parse(string? path, byte[]? password)
        {
            ArgumentNullException.ThrowIfNull(path);

            byte[] data = File.ReadAllBytes(path);
            if (data.Length == 0)
            {
                throw new Exception("The file does not contain any data.");
            }

            var backupType = GetBackupType(data);
            AuthenticatorProBackup? backup;
            switch (backupType)
            {
                case BackupType.Invalid:
                    throw new Exception(I18n.GetString("import.authpro.invalid"));
                case BackupType.LegacyEncryption:
                    throw new Exception(I18n.GetString("import.authpro.legacy"));
                case BackupType.StrongEncryption:
                    ArgumentNullException.ThrowIfNull(password);
                    backup = DecryptStrong(data, password);
                    break;
                case BackupType.NoEncryption:
                    var json = Encoding.UTF8.GetString(data);
                    backup = JsonSerializer.Deserialize<AuthenticatorProBackup>(json);
                    break;
                default:
                    throw new Exception("Unknown AuthenticatorPro backup type (internal error).");
            }

            if (backup == null || backup.Authenticators == null)
            {
                throw new Exception("The file does not contain any data.");
            }

            int total = 0,
                duplicate = 0,
                tokenID = 0;

            EncryptionHelper encryption = Auth.GetMainEncryptionHelper();
            foreach (var token in backup.Authenticators)
            {
                if (string.IsNullOrEmpty(token.Issuer))
                {
                    throw new Exception("Invalid AuthenticatorPro backup: No issuer found");
                }

                if (token.Secret == null)
                {
                    throw new Exception("Invalid AuthenticatorPro backup: No secret found");
                }

                string normalizedSecret = OTPUriParser.NormalizeSecret(token.Secret);

                if (!OTPUriParser.IsValidSecret(normalizedSecret))
                {
                    throw new Exception($"{I18n.GetString("td.invalidsecret")} ({token.Issuer})");
                }

                if (
                    token.Type != AuthenticatorProBackup.AuthenticatorType.Totp
                    && token.Type != AuthenticatorProBackup.AuthenticatorType.SteamOtp
                )
                {
                    throw new Exception(
                        $"Only TOTP tokens are supported. Backup contains {token.Type} token."
                    );
                }

                DBTOTPToken dbToken = new()
                {
                    Id = TokenManager.GetNextId(),
                    Issuer = token.Issuer,
                    EncryptedSecret = encryption.EncryptStringToBytes(normalizedSecret),
                    CreationTime = DateTime.Now,
                };

                if (token.Type == AuthenticatorProBackup.AuthenticatorType.SteamOtp)
                {
                    // For steam tokens, we force the issuer to be "Steam"
                    dbToken.Issuer = "Steam";
                }

                if (!string.IsNullOrEmpty(token.Username))
                {
                    dbToken.EncryptedUsername = encryption.EncryptStringToBytes(token.Username);
                }

                TotpIcon icon = IconManager.GetIcon(dbToken.Issuer, IconType.Any);
                if (icon != null && icon.Type != IconType.Default)
                {
                    dbToken.Icon = icon.Name;
                    dbToken.IconType = icon.Type;
                }

                if (token.Algorithm != null)
                {
                    dbToken.Algorithm = token.Algorithm switch
                    {
                        AuthenticatorProBackup.HashAlgorithm.Sha1 => TOTPAlgorithm.SHA1,
                        AuthenticatorProBackup.HashAlgorithm.Sha256 => TOTPAlgorithm.SHA256,
                        AuthenticatorProBackup.HashAlgorithm.Sha512 => TOTPAlgorithm.SHA512,
                        _ => throw new Exception(
                            $"Invalid AuthenticatorPro: Unsupported algorithm {token.Algorithm}"
                        ),
                    };
                }

                if (token.Digits != null)
                {
                    dbToken.Digits = token.Digits;
                }

                if (token.Period != null)
                {
                    dbToken.Period = token.Period;
                }

                total += 1;
                if (!TokenManager.AddToken(dbToken))
                {
                    duplicate += 1;
                }
                else
                {
                    tokenID = dbToken.Id;
                }
            }

            return (total, duplicate, tokenID);
        }

        public bool RequiresPassword(string? path)
        {
            ArgumentNullException.ThrowIfNull(path);

            byte[] data = File.ReadAllBytes(path);
            if (data.Length == 0)
            {
                throw new Exception("The Authenticator Pro backup file does not contain any data.");
            }
            return GetBackupType(data) == BackupType.StrongEncryption;
        }

        private static BackupType GetBackupType(byte[] data)
        {
            try
            {
                var json = Encoding.UTF8.GetString(data);
                var backup = JsonSerializer.Deserialize<AuthenticatorProBackup>(json);
                if (backup?.Authenticators == null)
                {
                    return BackupType.Invalid;
                }
                return BackupType.NoEncryption;
            }
            catch (Exception)
            {
                // ignore
            }

            ReadOnlySpan<byte> foundHeader = data.Take(LegacyHeader.Length).ToArray();
            if (foundHeader.SequenceEqual(Encoding.UTF8.GetBytes(LegacyHeader)))
            {
                return BackupType.LegacyEncryption;
            }
            if (foundHeader.SequenceEqual(Encoding.UTF8.GetBytes(StrongHeader)))
            {
                return BackupType.StrongEncryption;
            }
            return BackupType.Invalid;
        }

        private static AuthenticatorProBackup? DecryptStrong(byte[] data, byte[] password)
        {
            using var memoryStream = new MemoryStream(data);
            using var binaryReader = new BinaryReader(memoryStream);

            binaryReader.ReadBytes(StrongHeader.Length);
            byte[] salt = binaryReader.ReadBytes(SaltLength);
            ReadOnlySpan<byte> iv = binaryReader.ReadBytes(IvLength);
            ReadOnlySpan<byte> encryptedData = binaryReader.ReadBytes(
                data.Length - StrongHeader.Length - SaltLength - IvLength
            );

            var argon2id = new Konscious.Security.Cryptography.Argon2id(password)
            {
                DegreeOfParallelism = ArgonParallelism,
                Iterations = ArgonIterations,
                MemorySize = ArgonMemorySize,
                Salt = salt,
            };

            byte[] keyBytes = argon2id.GetBytes(KeyLength);

            if (!Aes256Gcm.IsSupported)
            {
                throw new Exception(
                    "AES256-GCM is not supported on this platform. The reason may be that your CPU does not support hardware-accelerated AES256-GCM encryption."
                );
            }

            Aes256Gcm aes = new();
            Key key = Key.Import(aes, keyBytes, KeyBlobFormat.RawSymmetricKey);

            byte[]? decryptedData =
                aes.Decrypt(key, iv, null, encryptedData)
                ?? throw new Exception(I18n.GetString("import.password.invalid"));
            var json = Encoding.UTF8.GetString(decryptedData);
            return JsonSerializer.Deserialize<AuthenticatorProBackup>(json);
        }
    }
}
