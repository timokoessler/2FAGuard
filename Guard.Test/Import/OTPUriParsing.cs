using Guard.WPF.Core.Import;
using Guard.WPF.Core.Models;

namespace Guard.Test.Import
{
    public class OTPUriParsing
    {
        [Fact]
        public void ParseSimpleUri()
        {
            string uri = "otpauth://totp/Example:User?secret=TEST&issuer=Example";
            OTPUri otpUri = OTPUriParser.Parse(uri);
            Assert.Equal("User", otpUri.Account);
            Assert.Equal("Example", otpUri.Issuer);
            Assert.Equal("TEST", otpUri.Secret);
            Assert.Equal(OtpUriType.TOTP, otpUri.Type);
        }

        [Fact]
        public void ParseUriWithDigitsAndPeriod()
        {
            string uri = "otpauth://totp/GitHub:User?secret=SECRET&digits=8&period=60";
            OTPUri otpUri = OTPUriParser.Parse(uri);
            Assert.Equal("User", otpUri.Account);
            Assert.Equal("GitHub", otpUri.Issuer);
            Assert.Equal("SECRET", otpUri.Secret);
            Assert.Equal(OtpUriType.TOTP, otpUri.Type);
            Assert.Equal(8, otpUri.Digits);
            Assert.Equal(60, otpUri.Period);
        }

        [Fact]
        public void ParseUriWithAlgorithm()
        {
            string uri = "otpauth://totp/ExampleUser?secret=SECRET&algorithm=SHA256&issuer=Test";
            OTPUri otpUri = OTPUriParser.Parse(uri);
            Assert.Equal("ExampleUser", otpUri.Account);
            Assert.Equal("Test", otpUri.Issuer);
            Assert.Equal("SECRET", otpUri.Secret);
            Assert.Equal(OtpUriType.TOTP, otpUri.Type);
            Assert.Equal(TOTPAlgorithm.SHA256, otpUri.Algorithm);
        }

        [Fact]
        public void ParseUriWithInvalidAlgorithm()
        {
            string uri = "otpauth://totp/ExampleUser?secret=SECRET&algorithm=SHA3-512";
            Exception ex = Assert.Throws<Exception>(() => OTPUriParser.Parse(uri));
            Assert.Equal("Invalid algorithm", ex.Message);
        }

        [Fact]
        public void ParseUriWithUriEncodedLabel()
        {
            string uri = "otpauth://totp/Example%3AUser?secret=SECRET";
            OTPUri otpUri = OTPUriParser.Parse(uri);
            Assert.Equal("User", otpUri.Account);
            Assert.Equal("Example", otpUri.Issuer);
            Assert.Equal("SECRET", otpUri.Secret);
            Assert.Equal(OtpUriType.TOTP, otpUri.Type);
        }

        [Fact]
        public void ParseCiscoMerakiUri()
        {
            string uri = "otpauth://totp/test%40example.com%2FMeraki?secret=ABCDEFG";
            OTPUri otpUri = OTPUriParser.Parse(uri);
            Assert.Equal("test@example.com", otpUri.Account);
            Assert.Equal("Meraki", otpUri.Issuer);
            Assert.Equal("ABCDEFG", otpUri.Secret);
            Assert.Equal(OtpUriType.TOTP, otpUri.Type);
        }
    }
}
