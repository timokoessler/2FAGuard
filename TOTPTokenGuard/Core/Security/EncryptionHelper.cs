using System.IO;
using System.Security.Cryptography;

namespace TOTPTokenGuard.Core.Security
{
    internal class EncryptionHelper
    {
        private const int Iterations = 600000;
        private const int SaltSize = 16;
        private const int KeySize = 32; // 256 bit
        private Aes aes;

        public EncryptionHelper(string password, string saltStr)
        {
            byte[] salt = Convert.FromBase64String(saltStr);
            using var deriveBytes = DeriveKey(password, salt);
            aes = Aes.Create();
            aes.Key = deriveBytes.GetBytes(KeySize);
            aes.IV =
                aes.BlockSize == 128
                    ? deriveBytes.GetBytes(aes.BlockSize / 8)
                    : deriveBytes.GetBytes(aes.BlockSize / 8);
            aes.Mode = CipherMode.CBC;
        }

        public string EncryptString(string plainText)
        {
            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var msEncrypt = new MemoryStream();
            using (
                var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)
            )
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }
            byte[] encryptedBytes = msEncrypt.ToArray();
            return Convert.ToBase64String(encryptedBytes);
        }

        public string DecryptString(string encryptedText)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

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

        private static Rfc2898DeriveBytes DeriveKey(string password, byte[] salt)
        {
            return new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA512);
        }

        public static string GetRandomBase64String(int count)
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(count));
        }
    }
}
