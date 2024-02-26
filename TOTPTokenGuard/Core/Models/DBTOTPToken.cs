using TOTPTokenGuard.Core.Icons;

namespace TOTPTokenGuard.Core.Models
{
    internal enum TOTPAlgorithm
    {
        SHA1,
        SHA256,
        SHA512
    }

    internal class DBTOTPToken
    {
        public required int Id { get; set; }
        public required string Issuer { get; set; }
        public string? Username { get; set; }
        public required string EncryptedSecret { get; set; }
        public TOTPAlgorithm? Algorithm { get; set; }
        public int? Digits { get; set; }
        public int? Period { get; set; }
        public string? Icon { get; set; }
        public IconManager.IconType? IconType { get; set; }
        public string? EncryptedNotes { get; set; }
        public long? CreationTime { get; set; }
    }
}
