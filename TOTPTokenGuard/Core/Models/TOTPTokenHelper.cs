using OtpNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOTPTokenGuard.Core.Security;

namespace TOTPTokenGuard.Core.Models
{
    internal class TOTPTokenHelper
    {

        private string decryptedSecret;
        private Totp totp;
        private DBTOTPToken dBTOTPToken;

        public TOTPTokenHelper(DBTOTPToken dbTOTPToken) {
            this.dBTOTPToken = dbTOTPToken;
            decryptedSecret = Auth.GetMainEncryptionHelper().DecryptString(dbTOTPToken.EncryptedSecret);
            byte[] secret = Base32Encoding.ToBytes(decryptedSecret);
            totp = new Totp(secret);
        }

        public int GetRemainingSeconds()
        {
            return totp.RemainingSeconds();
        }

        public string GenerateToken() {
            return totp.ComputeTotp();
        }

        public DBTOTPToken getInfo()
        {
            return dBTOTPToken;
        }

    }
}
