using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OtpNet;
using TOTPTokenGuard.Core.Security;

namespace TOTPTokenGuard.Core.Models
{
    internal class TOTPTokenHelper
    {
        private readonly string decryptedSecret;
        private readonly Totp totp;
        internal readonly DBTOTPToken dBToken;

        public TOTPTokenHelper(DBTOTPToken dBToken)
        {
            this.dBToken = dBToken;
            decryptedSecret = Auth.GetMainEncryptionHelper().DecryptString(dBToken.EncryptedSecret);
            byte[] secret = Base32Encoding.ToBytes(decryptedSecret);

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
