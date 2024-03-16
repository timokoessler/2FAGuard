using System.Security.Cryptography;
using System.Text;
using NSec.Cryptography;

namespace Guard.Core.Security
{
    internal class EncryptionHelper
    {
        public const int SaltSize = 16;
        private const int KeySize = 32; // 256 bit
        private const int NonceSize = 32; // 256 bit
        private readonly Aegis256 aegis;
        private readonly Key key;

        public EncryptionHelper(string password, string saltStr)
        {
            byte[] salt = Convert.FromBase64String(saltStr);
            byte[] keyBytes = DeriveKey(password, salt);
            aegis = new Aegis256();
            key = Key.Import(aegis, keyBytes, KeyBlobFormat.RawSymmetricKey);
        }

        public string EncryptString(string plainText)
        {
            return Convert.ToBase64String(EncryptBytes(Encoding.UTF8.GetBytes(plainText)));
        }

        public byte[] EncryptStringToBytes(string plainText)
        {
            return EncryptBytes(Encoding.UTF8.GetBytes(plainText));
        }

        public byte[] EncryptBytes(byte[] bytes)
        {
            byte[] nonce = GenerateNonce();
            byte[] encrypted = aegis.Encrypt(key, nonce, null, bytes);

            byte[] result = new byte[NonceSize + encrypted.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(encrypted, 0, result, NonceSize, encrypted.Length);

            return result;
        }

        public string DecryptString(string encryptedText)
        {
            return Encoding.UTF8.GetString(DecryptBytes(Convert.FromBase64String(encryptedText)));
        }

        public string DecryptBytesToString(byte[] allBytes)
        {
            return Encoding.UTF8.GetString(DecryptBytes(allBytes));
        }

        public byte[] DecryptBytes(byte[] allBytes)
        {
            byte[] nonce = new byte[NonceSize];
            Buffer.BlockCopy(allBytes, 0, nonce, 0, NonceSize);

            byte[] encrypted = new byte[allBytes.Length - NonceSize];
            Buffer.BlockCopy(allBytes, NonceSize, encrypted, 0, encrypted.Length);

            return aegis.Decrypt(key, nonce, null, encrypted)
                ?? throw new CryptographicException("Decryption failed (null)");
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

        private static byte[] GenerateNonce()
        {
            byte[] nonce;
            using (var rng = RandomNumberGenerator.Create())
            {
                nonce = new byte[NonceSize];
                rng.GetBytes(nonce);
            }
            return nonce;
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            var argon2id = new Konscious.Security.Cryptography.Argon2id(
                Encoding.UTF8.GetBytes(password)
            )
            {
                DegreeOfParallelism = 1,
                Iterations = 3,
                MemorySize = 67108,
                Salt = salt
            };

            return argon2id.GetBytes(KeySize);
        }

        public static string GetRandomBase64String(int count)
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(count));
        }

        public static string GetRandomHexString(int count)
        {
            return BitConverter
                .ToString(RandomNumberGenerator.GetBytes(count))
                .Replace("-", "")
                .ToLower();
        }
    }
}
