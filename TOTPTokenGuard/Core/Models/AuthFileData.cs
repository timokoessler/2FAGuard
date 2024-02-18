namespace TOTPTokenGuard.Core.Models
{
    internal class AuthFileData
    {
        public string? WindowsHelloProtectedKey { get; set; }
        public string? WindowsHelloChallenge { get; set; }
        public string? PasswordProtectedKey { get; set; }
        public string? ProtectedDbKey { get; set; }
        public string? InsecureMainKey { get; set; }
    }
}
