namespace TOTPTokenGuard.Core.Models
{
    internal enum TOTPAlgorithm
    {
        SHA1,
        SHA256,
        SHA512
    }

    internal class TOTPToken
    {
        public required int Id { get; set; }
        public required string Issuer { get; set; }
        public string? Username { get; set; }
        public required string EncryptedSecret { get; set; }
        public TOTPAlgorithm? Algorithm { get; set; }
        public int? Digits { get; set; }
        public int? Period { get; set; }
    }
}
