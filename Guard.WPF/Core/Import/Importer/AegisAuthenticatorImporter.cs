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
    internal class AegisAuthenticatorImporter : IImporter
    {
        public string Name => "AegisAuthenticator";
        public IImporter.ImportType Type => IImporter.ImportType.File;
        public string SupportedFileExtensions => "Aegis Authenticator Export (*.json) | *.json";

        private static bool IsEncryptedBackup(string jsonString)
        {
            JsonDocument doc = JsonDocument.Parse(jsonString);
            JsonElement root = doc.RootElement;
            JsonElement db = root.GetProperty("db");

            if (db.ValueKind == JsonValueKind.String)
            {
                return true;
            }
            else if (db.ValueKind == JsonValueKind.Object)
            {
                return false;
            }
            throw new Exception(
                "Invalid JSON format of Aegis Export (db is neither string nor object)"
            );
        }

        private readonly JsonSerializerOptions jsonSerializerOptions =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public bool RequiresPassword(string? path)
        {
            ArgumentNullException.ThrowIfNull(path);
            string jsonString = File.ReadAllText(path, Encoding.UTF8);
            return IsEncryptedBackup(jsonString);
        }

        // https://nsec.rocks/docs/api/nsec.cryptography.passwordbasedkeyderivationalgorithm

        public (int total, int duplicate, int tokenID) Parse(string? path, byte[]? password)
        {
            ArgumentNullException.ThrowIfNull(path);
            string jsonString = File.ReadAllText(path, Encoding.UTF8);

            AegisExport.Database? database;

            if (IsEncryptedBackup(jsonString))
            {
                // Encrypted backup
                ArgumentNullException.ThrowIfNull(password);

                AegisExport.Encrypted export =
                    JsonSerializer.Deserialize<AegisExport.Encrypted>(
                        jsonString,
                        jsonSerializerOptions
                    ) ?? throw new Exception("Invalid JSON format of encrypted Aegis Export");

                if (export.Version != 1)
                {
                    throw new Exception(
                        $"Unsupported Aegis Export version {export.Version}. Only version 1 is supported."
                    );
                }

                if (export.Header.Slots == null)
                {
                    throw new Exception(
                        "Invalid JSON format of encrypted Aegis Export (slots is null)"
                    );
                }

                byte[]? masterKey = null;

                foreach (AegisExport.HeaderSlot slot in export.Header.Slots)
                {
                    if (slot.Type != 1)
                    {
                        continue;
                    }
                    if (slot.Key_Params == null)
                    {
                        throw new Exception(
                            "Invalid JSON format of encrypted Aegis Export (key_params is null)"
                        );
                    }
                    if (slot.Key_Params.Nonce == null || slot.Key_Params.Tag == null)
                    {
                        throw new Exception(
                            "Invalid JSON format of encrypted Aegis Export (nonce or tag is null)"
                        );
                    }
                    if (slot.Key == null)
                    {
                        throw new Exception(
                            "Invalid JSON format of encrypted Aegis Export (key is null)"
                        );
                    }
                    if (slot.Salt == null)
                    {
                        throw new Exception(
                            "Invalid JSON format of encrypted Aegis Export (salt is null)"
                        );
                    }
                    if (slot.N == null || slot.R == null || slot.P == null)
                    {
                        throw new Exception(
                            "Invalid JSON format of encrypted Aegis Export (n, r or p is null)"
                        );
                    }

                    Scrypt scrypt =
                        new(
                            new ScryptParameters()
                            {
                                Cost = slot.N.Value,
                                BlockSize = slot.R.Value,
                                Parallelization = slot.P.Value,
                            }
                        );

                    try
                    {
                        byte[] keyBytes = scrypt.DeriveBytes(
                            password,
                            Convert.FromHexString(slot.Salt),
                            32
                        );
                        if (keyBytes.Length != 32)
                        {
                            throw new Exception("Invalid master key length");
                        }

                        Aes256Gcm masterKeyAes = new();
                        Key key = Key.Import(masterKeyAes, keyBytes, KeyBlobFormat.RawSymmetricKey);

                        byte[] encryptedMasterKey = Convert.FromHexString(slot.Key);
                        byte[] keyNonce = Convert.FromHexString(slot.Key_Params.Nonce);
                        byte[] keyTag = Convert.FromHexString(slot.Key_Params.Tag);
                        byte[] encryptedKeyWithTag = new byte[
                            encryptedMasterKey.Length + keyTag.Length
                        ];
                        Buffer.BlockCopy(
                            encryptedMasterKey,
                            0,
                            encryptedKeyWithTag,
                            0,
                            encryptedMasterKey.Length
                        );
                        Buffer.BlockCopy(
                            keyTag,
                            0,
                            encryptedKeyWithTag,
                            encryptedMasterKey.Length,
                            keyTag.Length
                        );

                        masterKey =
                            masterKeyAes.Decrypt(key, keyNonce, null, encryptedKeyWithTag)
                            ?? throw new Exception(
                                "Can not decrypt Aegis master key with provided password"
                            );
                        break;
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                if (masterKey == null)
                {
                    throw new Exception(I18n.GetString("import.password.invalid"));
                }

                if (export.Header.Params == null)
                {
                    throw new Exception(
                        "Invalid JSON format of encrypted Aegis Export (params is null)"
                    );
                }

                Aes256Gcm aes = new();
                Key masterKeyKey = Key.Import(aes, masterKey, KeyBlobFormat.RawSymmetricKey);
                byte[] encryptedDb = Convert.FromBase64String(export.Db);
                byte[] nonce = Convert.FromHexString(export.Header.Params.Nonce);
                byte[] tag = Convert.FromHexString(export.Header.Params.Tag);
                byte[] encryptedWithTag = new byte[encryptedDb.Length + tag.Length];
                Buffer.BlockCopy(encryptedDb, 0, encryptedWithTag, 0, encryptedDb.Length);
                Buffer.BlockCopy(tag, 0, encryptedWithTag, encryptedDb.Length, tag.Length);

                byte[] decryptedDb =
                    aes.Decrypt(masterKeyKey, nonce, null, encryptedWithTag)
                    ?? throw new Exception(
                        "Can not decrypt Aegis database with provided master key, but the entered password was correct"
                    );

                database =
                    JsonSerializer.Deserialize<AegisExport.Database>(
                        Encoding.UTF8.GetString(decryptedDb),
                        jsonSerializerOptions
                    ) ?? throw new Exception("Invalid JSON format of decrypted Aegis database");
            }
            else
            {
                // Unencrypted backup
                AegisExport.Plain export =
                    JsonSerializer.Deserialize<AegisExport.Plain>(jsonString, jsonSerializerOptions)
                    ?? throw new Exception("Invalid JSON format of plain Aegis Export");

                if (export.Version != 1)
                {
                    throw new Exception(
                        $"Unsupported Aegis Export version {export.Version}. Only version 1 is supported."
                    );
                }

                database = export.Db;
            }

            if (database.Version != 3)
            {
                throw new Exception(
                    $"Unsupported Aegis database version {database.Version}. Only version 3 is supported."
                );
            }

            int total = 0,
                duplicate = 0,
                tokenID = 0;

            EncryptionHelper encryption = Auth.GetMainEncryptionHelper();
            foreach (var token in database.Entries)
            {
                TotpIcon icon = IconManager.GetIcon(token.Issuer, IconType.Any);

                string normalizedSecret = OTPUriParser.NormalizeSecret(token.Info.Secret);

                if (!OTPUriParser.IsValidSecret(normalizedSecret))
                {
                    throw new Exception($"{I18n.GetString("td.invalidsecret")} ({token.Issuer})");
                }

                if (token.Type != "totp")
                {
                    throw new Exception(
                        $"Only TOTP tokens are supported. Backup contains {token.Type} token."
                    );
                }

                DBTOTPToken dbToken =
                    new()
                    {
                        Id = TokenManager.GetNextId(),
                        Issuer = string.IsNullOrEmpty(token.Issuer) ? "" : token.Issuer,
                        EncryptedSecret = encryption.EncryptStringToBytes(normalizedSecret),
                        CreationTime = DateTime.Now
                    };

                if (!string.IsNullOrEmpty(token.Name))
                {
                    dbToken.EncryptedUsername = encryption.EncryptStringToBytes(token.Name);
                }

                if (string.IsNullOrEmpty(token.Issuer) && string.IsNullOrEmpty(token.Name))
                {
                    throw new Exception("Token must have either issuer or name set.");
                }

                if (icon != null && icon.Type != IconType.Default)
                {
                    dbToken.Icon = icon.Name;
                    dbToken.IconType = icon.Type;
                }

                dbToken.Algorithm = token.Info.Algo.ToUpper() switch
                {
                    "SHA1" => TOTPAlgorithm.SHA1,
                    "SHA256" => TOTPAlgorithm.SHA256,
                    "SHA512" => TOTPAlgorithm.SHA512,
                    _
                        => throw new Exception(
                            $"Unsupported algorithm in Aegis export: {token.Info.Algo}"
                        ),
                };

                dbToken.Digits = token.Info.Digits;
                dbToken.Period = token.Info.Period;

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
    }
}
