namespace Guard.Core.Models
{
    public enum TOTPAlgorithm
    {
        SHA1,
        SHA256,
        SHA512,
    }

    public class DBTOTPToken
    {
        public required int Id { get; set; }
        public required string Issuer { get; set; }
        public byte[]? EncryptedUsername { get; set; }
        public required byte[] EncryptedSecret { get; set; }
        public TOTPAlgorithm? Algorithm { get; set; }
        public int? Digits { get; set; }
        public int? Period { get; set; }
        public string? Icon { get; set; }
        public IconType? IconType { get; set; }
        public byte[]? EncryptedNotes { get; set; }
        public DateTime? UpdatedTime { get; set; }
        public required DateTime CreationTime { get; set; }
    }
}
