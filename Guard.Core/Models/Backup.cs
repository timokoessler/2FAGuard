namespace Guard.Core.Models
{
    public class Backup
    {
        public class Token
        {
            public required string Issuer { get; set; }
            public string? Username { get; set; }
            public required string Secret { get; set; }
            public TOTPAlgorithm? Algorithm { get; set; }
            public int? Digits { get; set; }
            public int? Period { get; set; }
            public string? Icon { get; set; }
            public IconType? IconType { get; set; }
            public string? Notes { get; set; }
            public DateTime? UpdatedTime { get; set; }
            public required DateTime CreationTime { get; set; }
            public string? IconBase64 { get; set; }
        }

        public int Version { get; set; }
        public required Token[] Tokens { get; set; }
    }
}
