using System.Text;
using Guard.Core.Security;

namespace Guard.Test.Core
{
    public class Encryption
    {
        [Fact]
        public void TestSimpleEncryption()
        {
            EncryptionHelper encryptionHelper = new(
                Encoding.UTF8.GetBytes("password132"),
                EncryptionHelper.GenerateSaltBytes()
            );
            string toEncrypt = "testabc";
            string encrypted = encryptionHelper.EncryptString(toEncrypt);
            Assert.Equal(toEncrypt, encryptionHelper.DecryptString(encrypted));
        }

        [Fact]
        public void TestSimpleByteEncryption()
        {
            EncryptionHelper encryptionHelper = new(
                Encoding.UTF8.GetBytes("password1324"),
                EncryptionHelper.GenerateSaltBytes()
            );
            byte[] toEncrypt = EncryptionHelper.GetRandomBytes(32);
            byte[] encrypted = encryptionHelper.EncryptBytes(toEncrypt);
            Assert.Equal(toEncrypt, encryptionHelper.DecryptBytes(encrypted));
        }
    }
}
