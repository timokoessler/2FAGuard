using System.Windows.Controls;
using OtpNet;
using TOTPTokenGuard.Core.Models;

namespace TOTPTokenGuard.Views.UIComponents
{
    /// <summary>
    /// Interaktionslogik für TokenCard.xaml
    /// </summary>
    partial class TokenCard : UserControl
    {
        private TOTPToken tToken;
        private Totp totp;

        internal TokenCard(TOTPToken tToken)
        {
            InitializeComponent();

            this.tToken = tToken;
            // Todo decrypt secret
            byte[] secret = Base32Encoding.ToBytes(tToken.EncryptedSecret);
            totp = new Totp(secret);

            updateTokenText();
        }

        private void updateTokenText()
        {
            string tokenStr = totp.ComputeTotp();
            if (tokenStr.Length == 6)
            {
                tokenStr = tokenStr.Substring(0, 3) + " " + tokenStr.Substring(3);
            }
            else if (tokenStr.Length == 8)
            {
                tokenStr = tokenStr.Substring(0, 4) + " " + tokenStr.Substring(4);
            }
            TokenTextBlock.Text = tokenStr;
        }
    }
}
