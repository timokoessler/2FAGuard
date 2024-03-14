using System.IO;
using System.Text;
using System.Text.Json;
using Guard.Core.Models;
using Guard.Core.Security;

namespace Guard.Core.Import.Importer
{
    internal class BackupImporter : IImporter
    {
        public string Name => "Backup";
        public IImporter.ImportType Type => IImporter.ImportType.File;
        public string SupportedFileExtensions => "2FAGuard Backup (*.2fabackup) | *.2fabackup";
        private readonly byte[] prefix = Encoding.UTF8.GetBytes("2FAGuardBackupV1");

        public bool RequiresPassword(string? path) => true;

        public (int total, int duplicate, int tokenID) Parse(string? path, string? password)
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

            EncryptionHelper encryption = new(password, Convert.ToBase64String(salt));
            byte[] decryptedData = encryption.DecryptBytes(encryptedData);

            Backup backup =
                JsonSerializer.Deserialize<Backup>(decryptedData)
                ?? throw new Exception("Could not deserialize backup");

            // Todo: Import tokens

            return (backup.Tokens.Length, 0, 0);
        }
    }
}
