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
        private string decryptedSecret;
        private Totp totp;
        internal readonly DBTOTPToken dBToken;

        public TOTPTokenHelper(DBTOTPToken dBToken)
        {
            this.dBToken = dBToken;
            decryptedSecret = Auth.GetMainEncryptionHelper().DecryptString(dBToken.EncryptedSecret);
            byte[] secret = Base32Encoding.ToBytes(decryptedSecret);
            totp = new Totp(secret);
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
