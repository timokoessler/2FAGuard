using Guard.Core.Security;
using Guard.Core.Token;
using OtpNet;

namespace Guard.Core.Models
{
    public class TOTPTokenHelper
    {
        public readonly string DecryptedSecret;
        private readonly Totp totp;
        public readonly DBTOTPToken dBToken;
        public readonly string? Username;
        public readonly bool IsSteamToken;
        private readonly byte[] secret;

        public TOTPTokenHelper(DBTOTPToken dBToken)
        {
            this.dBToken = dBToken;
            EncryptionHelper encryption = Auth.GetMainEncryptionHelper();
            DecryptedSecret = encryption.DecryptBytesToString(dBToken.EncryptedSecret);
            if (dBToken.EncryptedUsername != null)
            {
                Username = encryption.DecryptBytesToString(dBToken.EncryptedUsername);
            }
            secret = Base32Encoding.ToBytes(DecryptedSecret);

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

            IsSteamToken = dBToken.Issuer.Equals("Steam", StringComparison.OrdinalIgnoreCase);

            totp = new Totp(secret, dBToken.Period ?? 30, hashMode, dBToken.Digits ?? 6);
        }

        public int GetRemainingSeconds()
        {
            return totp.RemainingSeconds();
        }

        public string GenerateToken()
        {
            if (IsSteamToken)
            {
                return SteamTokenGenerator.ComputeTotp(secret);
            }
            return totp.ComputeTotp();
        }
    }
}
