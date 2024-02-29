using System.IO;
using System.Security.Cryptography;
using NSec.Cryptography;

namespace Guard.Core.Security
{
    internal class EncryptionHelper
    {
        private const int SaltSize = 16;
        private const int KeySize = 32; // 256 bit
        private readonly Argon2id argon2Id;
        private readonly Aes aes;

        public EncryptionHelper(string password, string saltStr)
        {
            byte[] salt = Convert.FromBase64String(saltStr);
            argon2Id = new Argon2id(
                new Argon2Parameters
                {
                    DegreeOfParallelism = 1,
                    MemorySize = 12288,
                    NumberOfPasses = 3,
                }
            );
            byte[] keyBytes = DeriveKey(password, salt);
            aes = Aes.Create();
            aes.Key = keyBytes;
            aes.Mode = CipherMode.CBC;
        }

        public string EncryptString(string plainText)
        {
            byte[] iv = GenerateIV();
            aes.IV = iv;
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }
            byte[] encryptedBytes = msEncrypt.ToArray();

            // Combine IV and encrypted data
            byte[] result = new byte[iv.Length + encryptedBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, iv.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        public string DecryptString(string encryptedText)
        {
            byte[] allBytes = Convert.FromBase64String(encryptedText);

            // Extract IV from the beginning
            byte[] iv = new byte[aes.BlockSize / 8];
            Buffer.BlockCopy(allBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            byte[] encryptedBytes = new byte[allBytes.Length - iv.Length];
            Buffer.BlockCopy(allBytes, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var msDecrypt = new MemoryStream(encryptedBytes);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            return srDecrypt.ReadToEnd();
        }

        public static string GenerateSalt()
        {
            byte[] salt;
            using (var rng = RandomNumberGenerator.Create())
            {
                salt = new byte[SaltSize];
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        private byte[] DeriveKey(string password, byte[] salt)
        {
            return argon2Id.DeriveBytes(password, salt, KeySize);
        }

        private byte[] GenerateIV()
        {
            byte[] iv;
            using (var rng = RandomNumberGenerator.Create())
            {
                iv = new byte[aes.BlockSize / 8];
                rng.GetBytes(iv);
            }
            return iv;
        }

        public static string GetRandomBase64String(int count)
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(count));
        }
    }
}
