using System.IO;
using System.Text;
using System.Text.Json;
using Guard.Core.Models;
using Guard.Core.Security;

namespace Guard.WPF.Core.Export.Exporter
{
    internal class BackupExporter : IExporter
    {
        public string Name => "Backup";
        public IExporter.ExportType Type => IExporter.ExportType.File;
        public string ExportFileExtensions => "2FAGuard Backup (*.2fabackup) | *.2fabackup";

        public bool RequiresPassword() => true;

        private readonly byte[] prefix = Encoding.UTF8.GetBytes("2FAGuardBackupV1");

        public string GetDefaultFileName()
        {
            return $"2FAGuardBackup-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.2fabackup";
        }

        public async Task Export(string? path, byte[]? password)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(password);

            byte[] salt = EncryptionHelper.GenerateSaltBytes();
            EncryptionHelper encryption = new(password, salt);

            var tokenHelpers =
                await TokenManager.GetAllTokens()
                ?? throw new Exception(I18n.GetString("export.notokens"));
            List<Backup.Token> tokens = [];

            foreach (var tokenHelper in tokenHelpers)
            {
                Backup.Token token =
                    new()
                    {
                        Issuer = tokenHelper.dBToken.Issuer,
                        Username = tokenHelper.Username,
                        Secret = tokenHelper.DecryptedSecret,
                        Algorithm = tokenHelper.dBToken.Algorithm,
                        Digits = tokenHelper.dBToken.Digits,
                        Period = tokenHelper.dBToken.Period,
                        Icon = tokenHelper.dBToken.Icon,
                        IconType = tokenHelper.dBToken.IconType,
                        UpdatedTime = tokenHelper.dBToken.UpdatedTime,
                        CreationTime = tokenHelper.dBToken.CreationTime,
                    };
                if (tokenHelper.dBToken.EncryptedNotes != null)
                {
                    token.Notes = Auth.GetMainEncryptionHelper()
                        .DecryptBytesToString(tokenHelper.dBToken.EncryptedNotes);
                }
                tokens.Add(token);
            }

            Backup backup = new() { Version = 1, Tokens = [.. tokens], };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(backup);

            byte[] encryptedData = encryption.EncryptBytes(data);

            byte[] result = new byte[prefix.Length + salt.Length + encryptedData.Length];
            Buffer.BlockCopy(prefix, 0, result, 0, prefix.Length);
            Buffer.BlockCopy(salt, 0, result, prefix.Length, salt.Length);
            Buffer.BlockCopy(
                encryptedData,
                0,
                result,
                prefix.Length + salt.Length,
                encryptedData.Length
            );

            await File.WriteAllBytesAsync(path, result);
        }
    }
}
