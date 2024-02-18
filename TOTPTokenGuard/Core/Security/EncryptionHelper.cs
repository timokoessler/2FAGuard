using System.IO;
using System.Security.Cryptography;

namespace TOTPTokenGuard.Core.Security
{
    internal class EncryptionHelper
    {
        private const int Iterations = 600000;
        private const int SaltSize = 16;
        private const int KeySize = 32; // 256 bit

        public static string EncryptString(string plainText, string password)
        {
            byte[] salt;
            using (var rng = RandomNumberGenerator.Create())
            {
                salt = new byte[SaltSize];
                rng.GetBytes(salt);
            }

            using var deriveBytes = DeriveKey(password, salt);
            using var aes = Aes.Create();
            aes.Key = deriveBytes.GetBytes(KeySize);
            aes.IV =
                aes.BlockSize == 128
                    ? deriveBytes.GetBytes(aes.BlockSize / 8)
                    : deriveBytes.GetBytes(aes.BlockSize / 8);
            aes.Mode = CipherMode.CBC;

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var msEncrypt = new MemoryStream())
            {
                using (
                    var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)
                )
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                byte[] encryptedBytes = msEncrypt.ToArray();
                byte[] resultBytes = new byte[salt.Length + encryptedBytes.Length];
                Buffer.BlockCopy(salt, 0, resultBytes, 0, salt.Length);
                Buffer.BlockCopy(
                    encryptedBytes,
                    0,
                    resultBytes,
                    salt.Length,
                    encryptedBytes.Length
                );
                return Convert.ToBase64String(resultBytes);
            }
        }

        public static string DecryptString(string cipherText, string password)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] salt = new byte[SaltSize];
            byte[] encryptedBytes = new byte[cipherBytes.Length - salt.Length];

            Buffer.BlockCopy(cipherBytes, 0, salt, 0, salt.Length);
            Buffer.BlockCopy(cipherBytes, salt.Length, encryptedBytes, 0, encryptedBytes.Length);

            using var deriveBytes = DeriveKey(password, salt);
            using (var aes = Aes.Create())
            {
                aes.Key = deriveBytes.GetBytes(KeySize);
                aes.IV =
                    aes.BlockSize == 128
                        ? deriveBytes.GetBytes(aes.BlockSize / 8)
                        : deriveBytes.GetBytes(aes.BlockSize / 8);
                aes.Mode = CipherMode.CBC;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var msDecrypt = new MemoryStream(encryptedBytes))
                using (
                    var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)
                )
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
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
