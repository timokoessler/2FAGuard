using Guard.WPF.Core.Import;
using Guard.WPF.Core.Models;

namespace Guard.Test.Import
{
    public class GoogleAuthMigration
    {
        [Fact]
        public void ParseMigrationUri()
        {
            string uri =
                "otpauth-migration://offline?data=Ci0KEBfNhW48%2BTYHuJJjaGSVaDgSDHRlc3RAdGVzdC5kZRoFQXBwbGUgASgBMAIKLwoUULGGAD3E0ueYHAy7s69SLeV1tHISCXVzZXJAdGVzdBoGVG9rZW4yIAEoATACEAEYASAAKJbEnKb%2B%2F%2F%2F%2F%2FwE%3D";
            List<OTPUri> otpUris = GoogleAuthenticator.Parse(uri);
            Assert.Equal(2, otpUris.Count);
            Assert.Equal("Apple", otpUris[0].Issuer);
            Assert.Equal("test@test.de", otpUris[0].Account);
            Assert.Equal("C7GYK3R47E3APOESMNUGJFLIHA======", otpUris[0].Secret);
            Assert.Equal(OtpUriType.TOTP, otpUris[0].Type);
            Assert.Equal(TOTPAlgorithm.SHA1, otpUris[0].Algorithm);
            Assert.Equal("Token2", otpUris[1].Issuer);
            Assert.Equal("user@test", otpUris[1].Account);
            Assert.Equal("KCYYMAB5YTJOPGA4BS53HL2SFXSXLNDS", otpUris[1].Secret);
            Assert.Equal(OtpUriType.TOTP, otpUris[1].Type);
            Assert.Equal(TOTPAlgorithm.SHA1, otpUris[1].Algorithm);
        }
    }
}
