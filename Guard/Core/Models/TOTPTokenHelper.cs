using Guard.Core.Security;
using OtpNet;

namespace Guard.Core.Models
{
    internal class TOTPTokenHelper
    {
        internal readonly string DecryptedSecret;
        private readonly Totp totp;
        internal readonly DBTOTPToken dBToken;
        internal readonly string? Username;

        public TOTPTokenHelper(DBTOTPToken dBToken)
        {
            this.dBToken = dBToken;
            EncryptionHelper encryption = Auth.GetMainEncryptionHelper();
            DecryptedSecret = encryption.DecryptBytesToString(dBToken.EncryptedSecret);
            if (dBToken.EncryptedUsername != null)
            {
                Username = encryption.DecryptBytesToString(dBToken.EncryptedUsername);
            }
            byte[] secret = Base32Encoding.ToBytes(DecryptedSecret);

            OtpHashMode hashMode = OtpHashMode.Sha1;
            if (dBToken.Algorithm != null)
            {
                if (dBToken.Algorithm == TOTPAlgorithm.SHA256)
                {
                    hashMode = OtpHashMode.Sha256;
                }
                else if (dBToken.Algorithm == TOTPAlgorithm.SHA512)
                {
                    hashMode = OtpHashMode.Sha512;
                }
            }

            totp = new Totp(secret, dBToken.Period ?? 30, hashMode, dBToken.Digits ?? 6);
        }

        public int GetRemainingSeconds()
        {
            return totp.RemainingSeconds();
        }

        public string GenerateToken()
        {
            return totp.ComputeTotp();
        }
    }
}
