using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Guard.Core.Icons;
using Guard.Core.Models;
using Guard.Core.Security;

namespace Guard.Core.Import.Importer
{
    internal class BitwardenImporter : IImporter
    {
        public string Name => "Bitwarden";
        public IImporter.ImportType Type => IImporter.ImportType.File;
        public string SupportedFileExtensions => "Bitwarden Export (*.json) | *.json";
        private static readonly int KeySize = 32; // 256 bits

        public bool RequiresPassword(string? path)
        {
            ArgumentNullException.ThrowIfNull(path);
            BitwardenExportFile exportFile = ParseFile(path);
            return GetEncryptionType(exportFile) == BitwardenEncryptionType.Password;
        }

        private readonly JsonSerializerOptions jsonSerializerOptions =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

        private enum BitwardenTOTPType
        {
            Secret,
            Uri
        }

        private enum BitwardenEncryptionType
        {
            None,
            Password,
            Account
        }

        private BitwardenExportFile ParseFile(string path)
        {
            using FileStream stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<BitwardenExportFile>(stream, jsonSerializerOptions)
                ?? throw new Exception("Could not parse Bitwarden export file");
        }

        private static BitwardenEncryptionType GetEncryptionType(BitwardenExportFile exportFile)
        {
            if (exportFile.Encrypted == null || exportFile.Encrypted == false)
            {
                return BitwardenEncryptionType.None;
            }
            if (exportFile.PasswordProtected == true)
            {
                return BitwardenEncryptionType.Password;
            }
            return BitwardenEncryptionType.Account;
        }

        public (int total, int duplicate, int tokenID) Parse(string? path, byte[]? password)
        {
            ArgumentNullException.ThrowIfNull(path);
            BitwardenExportFile exportFile = ParseFile(path);

            var encryptionType = GetEncryptionType(exportFile);

            if (encryptionType == BitwardenEncryptionType.Account)
            {
                throw new Exception(I18n.GetString("i.import.bitwarden.accountencryption"));
            }

            BitwardenExportFile.Item[]? items;

            if (encryptionType == BitwardenEncryptionType.Password)
            {
                ArgumentNullException.ThrowIfNull(password);
                items = DecryptItems(exportFile, password);
            }
            else
            {
                items = exportFile.Items;
            }

            if (items == null)
            {
                throw new Exception("Invalid Bitwarden export file: No items found");
            }

            int total = 0,
                duplicate = 0,
                tokenID = 0;

            EncryptionHelper encryption = Auth.GetMainEncryptionHelper();

            foreach (BitwardenExportFile.Item item in items)
            {
                if (item.Login == null || item.Login.Totp == null)
                {
                    continue;
                }
                string totp = item.Login.Totp;

                BitwardenTOTPType exportTotpType = totp.StartsWith("otpauth://")
                    ? BitwardenTOTPType.Uri
                    : BitwardenTOTPType.Secret;

                DBTOTPToken dbToken;
                if (exportTotpType == BitwardenTOTPType.Uri)
                {
                    OTPUri otpUri = OTPUriParser.Parse(totp);
                    dbToken = OTPUriParser.ConvertToDBToken(otpUri);
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
                else
                {
                    if (item.Name == null)
                    {
                        throw new Exception("Invalid Bitwarden export file: No item name found");
                    }

                    IconManager.TotpIcon icon = IconManager.GetIcon(
                        item.Name,
                        IconManager.IconType.Any
                    );

                    string normalizedSecret = OTPUriParser.NormalizeSecret(item.Login.Totp);

                    if (!OTPUriParser.IsValidSecret(normalizedSecret))
                    {
                        throw new Exception($"{I18n.GetString("td.invalidsecret")} ({item.Name})");
                    }

                    dbToken = new()
                    {
                        Id = TokenManager.GetNextId(),
                        Issuer = item.Name,
                        EncryptedSecret = encryption.EncryptStringToBytes(normalizedSecret),
                        CreationTime = DateTime.Now
                    };

                    if (item.Login.Username != null)
                    {
                        dbToken.EncryptedUsername = encryption.EncryptStringToBytes(
                            item.Login.Username
                        );
                    }

                    if (icon != null && icon.Type != IconManager.IconType.Default)
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
                }
            }

            return (total, duplicate, tokenID);
        }

        private BitwardenExportFile.Item[]? DecryptItems(
            BitwardenExportFile exportFile,
            byte[] password
        )
        {
            ArgumentNullException.ThrowIfNull(exportFile.Data);

            var parts = exportFile.Data.Split(".", 2)[1].Split("|");
            if (parts.Length < 3)
            {
                throw new ArgumentException(
                    "Data of Bitwarden export file is invalid (missing parts)"
                );
            }

            byte[] iv = Convert.FromBase64String(parts[0]);
            byte[] encrypted = Convert.FromBase64String(parts[1]);
            byte[] mac = Convert.FromBase64String(parts[2]);
            byte[] key = DeriveKey(exportFile, password);
            byte[] encryptionKey = HKDF.Expand(
                HashAlgorithmName.SHA256,
                key,
                KeySize,
                Encoding.UTF8.GetBytes("enc")
            );
            byte[] macKey = HKDF.Expand(
                HashAlgorithmName.SHA256,
                key,
                KeySize,
                Encoding.UTF8.GetBytes("mac")
            );

            // Validate MAC
            var hmacHashContent = new byte[iv.Length + encrypted.Length];
            Buffer.BlockCopy(iv, 0, hmacHashContent, 0, iv.Length);
            Buffer.BlockCopy(encrypted, 0, hmacHashContent, iv.Length, encrypted.Length);
            using var hmac = new HMACSHA256(macKey);
            var hash = hmac.ComputeHash(hmacHashContent);
            if (!hash.SequenceEqual(mac))
            {
                throw new Exception(I18n.GetString("import.password.invalid"));
            }

            // Decrypt
            using Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = encryptionKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(encrypted);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            string json = srDecrypt.ReadToEnd();
            BitwardenExportFile decryptedFile =
                JsonSerializer.Deserialize<BitwardenExportFile>(json, jsonSerializerOptions)
                ?? throw new Exception("Could not parse Bitwarden export file after decryption");
            return decryptedFile.Items;
        }

        private static byte[] DeriveKey(BitwardenExportFile exportFile, byte[] password)
        {
            int iterations =
                exportFile.KdfIterations
                ?? throw new Exception("KDF iterations not found in Bitwarden export file");

            byte[] salt = Encoding.UTF8.GetBytes(
                exportFile.Salt ?? throw new Exception("Salt not found in Bitwarden export file")
            );

            if (exportFile.KdfType == BitwardenExportFile.BWKdfType.Pbkdf2)
            {
                return new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256
                ).GetBytes(KeySize);
            }
            else if (exportFile.KdfType == BitwardenExportFile.BWKdfType.Argon2id)
            {
                int parallelism =
                    exportFile.KdfParallelism
                    ?? throw new Exception("KDF parallelism not found in Bitwarden export file");
                int memorySize =
                    exportFile.KdfMemory
                    ?? throw new Exception("KDF memory size not found in Bitwarden export file");
                var argon2id = new Konscious.Security.Cryptography.Argon2id(password)
                {
                    DegreeOfParallelism = parallelism,
                    Iterations = iterations,
                    MemorySize = memorySize * 1024, // Convert to KB
                    Salt = SHA256.HashData(salt)
                };

                return argon2id.GetBytes(KeySize);
            }
            else
            {
                throw new Exception("Unsupported KDF type in Bitwarden export file");
            }
        }
    }
}
