using Guard.Core.Models;
using Guard.Core.Security;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Guard.Core.Import.Importer
{
    internal class BackupImporter : IImporter
    {
        public string Name => "Backup";
        public IImporter.ImportType Type => IImporter.ImportType.File;
        public string SupportedFileExtensions => "2FAGuard Backup (*.2fabackup) | *.2fabackup";
        private readonly byte[] prefix = Encoding.UTF8.GetBytes("2FAGuardBackupV1");

        public bool RequiresPassword(string? path) => true;

        public (int total, int duplicate, int tokenID) Parse(string? path, byte[]? password)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(password);

            byte[] data = File.ReadAllBytes(path);
            if (data.Length == 0)
            {
                throw new Exception("The file does not contain any data.");
            }

            if (!data.Take(prefix.Length).SequenceEqual(prefix))
            {
                throw new Exception("The file is not a valid 2FAGuard backup.");
            }

            byte[] salt = new byte[EncryptionHelper.SaltSize];
            Buffer.BlockCopy(data, prefix.Length, salt, 0, EncryptionHelper.SaltSize);

            byte[] encryptedData = new byte[
                data.Length - prefix.Length - EncryptionHelper.SaltSize
            ];
            Buffer.BlockCopy(
                data,
                prefix.Length + EncryptionHelper.SaltSize,
                encryptedData,
                0,
                encryptedData.Length
            );

            EncryptionHelper backupEncryption = new(password, salt);
            byte[] decryptedData = backupEncryption.DecryptBytes(encryptedData);

            Backup backup =
                JsonSerializer.Deserialize<Backup>(decryptedData)
                ?? throw new Exception("Could not deserialize backup");

            EncryptionHelper internalEncryption = Auth.GetMainEncryptionHelper();

            int duplicate = 0,
                tokenID = 0;

            foreach (var token in backup.Tokens)
            {
                if (TokenManager.IsDuplicate(token.Secret))
                {
                    duplicate++;
                    continue;
                }

                tokenID = TokenManager.GetNextId();
                DBTOTPToken dbToken = new()
                {
                    Id = tokenID,
                    Issuer = token.Issuer,
                    EncryptedUsername = token.Username != null
                        ? internalEncryption.EncryptStringToBytes(token.Username)
                        : null,
                    EncryptedSecret = internalEncryption.EncryptStringToBytes(token.Secret),
                    Algorithm = token.Algorithm,
                    Digits = token.Digits,
                    Period = token.Period,
                    Icon = token.Icon,
                    IconType = token.IconType,
                    UpdatedTime = token.UpdatedTime,
                    CreationTime = token.CreationTime,
                    EncryptedNotes = token.Notes != null
                        ? internalEncryption.EncryptStringToBytes(token.Notes)
                        : null
                };
                if (!TokenManager.AddToken(dbToken))
                {
                    throw new Exception("Failed to add token");
                }
            }

            return (backup.Tokens.Length, duplicate, tokenID);
        }
    }
}
