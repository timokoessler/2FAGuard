using System.IO;
using System.Text;
using System.Text.Json;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.WPF.Core.Models;
using NSec.Cryptography;

namespace Guard.WPF.Core.Export.Exporter
{
    internal class AuthenticatorProExporter : IExporter
    {
        public string Name => "Stratum";

        public IExporter.ExportType Type => IExporter.ExportType.File;
        public string ExportFileExtensions => "Stratum Backup (*.stratum) | *.stratum";

        public bool RequiresPassword() => true;

        public string GetDefaultFileName()
        {
            return $"2FAGuard-Stratum-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.stratum";
        }

        private const string StrongHeader = "AUTHENTICATORPRO";

        private const int ArgonParallelism = 4;
        private const int ArgonIterations = 3;
        private const int ArgonMemorySize = 65536;

        private const int SaltLength = 16;
        private const int KeyLength = 32;
        private const int IvLength = 12;

        public async Task Export(string? path, byte[]? password)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(password);

            var tokenHelpers =
                await TokenManager.GetAllTokens()
                ?? throw new Exception(I18n.GetString("export.notokens"));
            List<AuthenticatorProBackup.Authenticator> authenticators = [];

            foreach (var tokenHelper in tokenHelpers)
            {
                AuthenticatorProBackup.Authenticator authenticator =
                    new()
                    {
                        Issuer = tokenHelper.dBToken.Issuer,
                        Username = tokenHelper.Username,
                        Secret = tokenHelper.DecryptedSecret,
                        Digits = tokenHelper.dBToken.Digits ?? 6,
                        Period = tokenHelper.dBToken.Period ?? 30,
                        Type = AuthenticatorProBackup.AuthenticatorType.Totp,
                        CopyCount = 0,
                        Counter = 0,
                        Ranking = 0,
                        Pin = null,
                        Icon = null,
                    };

                if (tokenHelper.dBToken.Algorithm != null)
                {
                    authenticator.Algorithm = tokenHelper.dBToken.Algorithm switch
                    {
                        TOTPAlgorithm.SHA1 => AuthenticatorProBackup.HashAlgorithm.Sha1,
                        TOTPAlgorithm.SHA256 => AuthenticatorProBackup.HashAlgorithm.Sha256,
                        TOTPAlgorithm.SHA512 => AuthenticatorProBackup.HashAlgorithm.Sha512,
                        _
                            => throw new Exception(
                                $"Invalid algorithm {tokenHelper.dBToken.Algorithm}"
                            ),
                    };
                }
                else
                {
                    authenticator.Algorithm = AuthenticatorProBackup.HashAlgorithm.Sha1;
                }

                authenticators.Add(authenticator);
            }

            AuthenticatorProBackup authProBackup =
                new()
                {
                    Authenticators = [.. authenticators],
                    Categories = [],
                    AuthenticatorCategories = [],
                    CustomIcons = []
                };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(authProBackup);
            byte[] salt = EncryptionHelper.GetRandomBytes(SaltLength);
            byte[] nonce = EncryptionHelper.GetRandomBytes(IvLength);

            var argon2id = new Konscious.Security.Cryptography.Argon2id(password)
            {
                DegreeOfParallelism = ArgonParallelism,
                Iterations = ArgonIterations,
                MemorySize = ArgonMemorySize,
                Salt = salt
            };

            byte[] keyBytes = argon2id.GetBytes(KeyLength);

            if (!Aes256Gcm.IsSupported)
            {
                throw new Exception(
                    "This platform does not support hardware-accelerated AES (GCM) encryption that is required to import this file."
                );
            }

            Aes256Gcm aes = new();
            Key key = Key.Import(aes, keyBytes, KeyBlobFormat.RawSymmetricKey);
            byte[] encrypted = aes.Encrypt(key, nonce, null, data);

            byte[] strongHeaderBytes = Encoding.UTF8.GetBytes(StrongHeader);

            byte[] result = new byte[
                strongHeaderBytes.Length + salt.Length + nonce.Length + encrypted.Length
            ];
            Buffer.BlockCopy(strongHeaderBytes, 0, result, 0, strongHeaderBytes.Length);
            Buffer.BlockCopy(salt, 0, result, strongHeaderBytes.Length, salt.Length);
            Buffer.BlockCopy(
                nonce,
                0,
                result,
                strongHeaderBytes.Length + salt.Length,
                nonce.Length
            );
            Buffer.BlockCopy(
                encrypted,
                0,
                result,
                strongHeaderBytes.Length + salt.Length + nonce.Length,
                encrypted.Length
            );

            await File.WriteAllBytesAsync(path, result);
        }
    }
}
