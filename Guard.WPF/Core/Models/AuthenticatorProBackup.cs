namespace Guard.WPF.Core.Models
{
    internal class AuthenticatorProBackup
    {
        public enum AuthenticatorType
        {
            Hotp = 1,
            Totp = 2,
            MobileOtp = 3,
            SteamOtp = 4,
            YandexOtp = 5
        }

        public enum HashAlgorithm
        {
            Sha1 = 0,
            Sha256 = 1,
            Sha512 = 2
        }

        public class Authenticator
        {
            public string? Issuer { get; set; }
            public AuthenticatorType? Type { get; set; }
            public string? Username { get; set; }
            public string? Secret { get; set; }
            public HashAlgorithm? Algorithm { get; set; }
            public int? Digits { get; set; }
            public int? Period { get; set; }
            public int? Counter { get; set; }
            public int? CopyCount { get; set; }
            public int? Ranking { get; set; }
            public string? Icon { get; set; }
            public string? Pin { get; set; }
        }

        public IEnumerable<Authenticator>? Authenticators { get; set; }
        public IEnumerable<object>? Categories { get; set; }
        public IEnumerable<object>? AuthenticatorCategories { get; set; }
        public IEnumerable<object>? CustomIcons { get; set; }
    }
}
