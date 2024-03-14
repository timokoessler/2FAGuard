using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Core.Storage;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Guard.Core.Export.Exporter
{
    internal class BackupExporter : IExporter
    {
        public string Name => "Backup";
        public IExporter.ExportType Type => IExporter.ExportType.File;
        public string ExportFileExtensions => "2FAGuard Backup (*.2fabackup) | *.2fabackup";
        public bool RequiresPassword() => true;
        private readonly byte[] prefix = Encoding.UTF8.GetBytes("2FAGuardBackupV1");

        public async void Export(string? path, string? password)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(password);

            Backup backup = new()
            {
                Version = 1,
                Tokens = [.. Database.GetAllTokens()]
            };

            if (backup.Tokens.Length == 0)
            {
                throw new Exception("No tokens to export");
            }

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(backup);

            string salt = EncryptionHelper.GenerateSalt();
            EncryptionHelper encryption = new(password, salt);

            byte[] encryptedData = encryption.EncryptBytes(data);
            byte[] saltBytes = Convert.FromBase64String(salt);

            byte[] result = new byte[prefix.Length + saltBytes.Length + encryptedData.Length];
            Buffer.BlockCopy(prefix, 0, result, 0, prefix.Length);
            Buffer.BlockCopy(saltBytes, 0, result, prefix.Length, saltBytes.Length);
            Buffer.BlockCopy(encryptedData, 0, result, prefix.Length + saltBytes.Length, encryptedData.Length);

            await File.WriteAllBytesAsync(path, result);
        }
    }
}
