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
    class ProtonAuthenticatorImporter : IImporter
    {
        public string Name => "ProtonAuthenticator";
        public IImporter.ImportType Type => IImporter.ImportType.File;
        public string SupportedFileExtensions => "Proton Authenticator Export (*.json) | *.json";

        private readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public bool RequiresPassword(string? path)
        {
            ArgumentNullException.ThrowIfNull(path);
            string jsonString = File.ReadAllText(path, Encoding.UTF8);
            return IsEncryptedBackup(jsonString);
        }

        private static bool IsEncryptedBackup(string jsonString)
        {
            JsonDocument doc = JsonDocument.Parse(jsonString);
            JsonElement root = doc.RootElement;

            root.TryGetProperty("entries", out JsonElement entries);

            if (entries.ValueKind == JsonValueKind.Array)
            {
                return false;
            }

            root.TryGetProperty("content", out JsonElement content);
            if (content.ValueKind == JsonValueKind.String)
            {
                return true;
            }

            throw new Exception(
                "Invalid JSON format of Proton Authenticator export. Expected either 'entries' as an array or 'content' as a string."
            );
        }

        public (int total, int duplicate, int tokenID) Parse(string? path, byte[]? password)
        {
            ArgumentNullException.ThrowIfNull(path);
            string jsonString = File.ReadAllText(path, Encoding.UTF8);

            if (IsEncryptedBackup(jsonString))
            {
                if (password == null)
                {
                    throw new Exception(
                        "Password is required for encrypted Proton Authenticator backup."
                    );
                }

                return ParseEncrypted(jsonString, password);
            }
            else
            {
                return ParseUnencrypted(jsonString);
            }
        }

        private (int total, int duplicate, int tokenID) ParseUnencrypted(string jsonString)
        {
            ProtonAuthenticatorExport.UnencryptedExport export =
                JsonSerializer.Deserialize<ProtonAuthenticatorExport.UnencryptedExport>(
                    jsonString,
                    jsonSerializerOptions
                )!;
            return ParseEntries(export.Entries);
        }

        private (int total, int duplicate, int tokenID) ParseEntries(
            ProtonAuthenticatorExport.ExportEntry[] entries
        )
        {
            if (entries == null || entries.Length == 0)
            {
                throw new Exception("No entries found in the Proton Authenticator export.");
            }

            int total = 0,
                duplicate = 0,
                tokenID = 0;

            EncryptionHelper encryption = Auth.GetMainEncryptionHelper();

            foreach (ProtonAuthenticatorExport.ExportEntry entry in entries)
            {
                if (entry.Content.EntryType.ToLowerInvariant() == "totp")
                {
                    OTPUri otpUri = OTPUriParser.Parse(entry.Content.Uri, true);
                    if (string.IsNullOrEmpty(otpUri.Issuer))
                    {
                        otpUri.Issuer = "";
                    }

                    DBTOTPToken dbToken = OTPUriParser.ConvertToDBToken(otpUri);

                    if (!string.IsNullOrEmpty(entry.Content.Name))
                    {
                        dbToken.EncryptedUsername = encryption.EncryptStringToBytes(
                            entry.Content.Name
                        );
                    }

                    TotpIcon icon = IconManager.GetIcon(dbToken.Issuer, IconType.Any);

                    if (icon != null && icon.Type != IconType.Default)
                    {
                        dbToken.Icon = icon.Name;
                        dbToken.IconType = icon.Type;
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

                    continue;
                }

                if (entry.Content.EntryType.ToLowerInvariant() == "steam")
                {
                    string uri = entry.Content.Uri;
                    if (!uri.StartsWith("steam://", StringComparison.Ordinal))
                    {
                        throw new Exception(
                            "Invalid URI format for Steam entry, expected to start with 'steam://'."
                        );
                    }
                    string secretPart = uri.Substring("steam://".Length);

                    DBTOTPToken dbToken = new()
                    {
                        Id = TokenManager.GetNextId(),
                        Issuer = "Steam",
                        EncryptedSecret = encryption.EncryptStringToBytes(
                            OTPUriParser.NormalizeSecret(secretPart)
                        ),
                        Digits = 5,
                        Period = 30,
                        CreationTime = DateTime.Now,
                        Icon = "Steam",
                        IconType = IconType.SimpleIcons,
                    };

                    if (!string.IsNullOrEmpty(entry.Content.Name))
                    {
                        dbToken.EncryptedUsername = encryption.EncryptStringToBytes(
                            entry.Content.Name
                        );
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

                    continue;
                }

                throw new Exception(
                    $"Unsupported entry type '{entry.Content.EntryType}' in Proton Authenticator export."
                );
            }
            return (total, duplicate, tokenID);
        }

        private (int total, int duplicate, int tokenID) ParseEncrypted(
            string jsonString,
            byte[] password
        )
        {
            if (!Aes256Gcm.IsSupported)
            {
                throw new Exception(
                    "AES256-GCM is not supported on this platform. The reason may be that your CPU does not support hardware-accelerated AES256-GCM encryption."
                );
            }

            ProtonAuthenticatorExport.EncryptedExport encryptedExport =
                JsonSerializer.Deserialize<ProtonAuthenticatorExport.EncryptedExport>(
                    jsonString,
                    jsonSerializerOptions
                )!;

            byte[] saltBytes = Convert.FromBase64String(encryptedExport.Salt);
            byte[] encryptedContentBytes = Convert.FromBase64String(encryptedExport.Content);

            var argon2id = new Konscious.Security.Cryptography.Argon2id(password)
            {
                DegreeOfParallelism = 1,
                Iterations = 2,
                MemorySize = 19 * 1024, // Convert to KB
                Salt = saltBytes,
            };

            byte[] keyBytes = argon2id.GetBytes(32);

            Aes256Gcm aes = new();
            Key key = Key.Import(aes, keyBytes, KeyBlobFormat.RawSymmetricKey);

            byte[] nonce = [.. encryptedContentBytes.Take(12)];
            byte[] ciphertext = [.. encryptedContentBytes.Skip(12)];
            byte[] aad = Encoding.UTF8.GetBytes("proton.authenticator.export.v1");

            byte[] decrypted =
                aes.Decrypt(key, nonce, aad, ciphertext)
                ?? throw new Exception("Decryption failed. The password may be incorrect.");

            string decryptedJson = Encoding.UTF8.GetString(decrypted);

            ProtonAuthenticatorExport.UnencryptedExport export =
                JsonSerializer.Deserialize<ProtonAuthenticatorExport.UnencryptedExport>(
                    decryptedJson,
                    jsonSerializerOptions
                )!;

            return ParseEntries(export.Entries);
        }
    }
}
