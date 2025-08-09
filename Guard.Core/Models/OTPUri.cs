namespace Guard.Core.Models
{
    public enum OtpUriType
    {
        TOTP,
        HOTP,
    }

    public class OTPUri
    {
        public OtpUriType? Type { get; set; }
        public string? Issuer { get; set; }
        public string? Secret { get; set; }
        public string? Account { get; set; }
        public TOTPAlgorithm? Algorithm { get; set; }
        public int? Digits { get; set; }
        public int? Period { get; set; }
    }
}
