namespace Guard.Core.Models
{
    internal class TwoFasBackup
    {

        internal class ServiceOTP
        {
            public string? Link { get; set; }
            public string? Label { get; set; }
            public string? Account { get; set; }
            public string? Issuer { get; set; }
            public int? Digits { get; set; }
            public int? Period { get; set; }
            public string? Algorithm { get; set; }
            public string? TokenType { get; set; }
            public string? Source { get; set; }
        }

        internal class Service
        {
            public string? Name { get; set; }
            public string? Secret { get; set; }
            public ulong? UpdatedAt { get; set; }
            public ServiceOTP? OTP { get; set; }
        }

        public int SchemaVersion { get; set; }
        public int AppVersionCode { get; set; }
        public string? AppVersionName { get; set; }
        public string? AppOrigin { get; set; }
        public Service[]? Services { get; set; }
        public string? ServicesEncrypted { get; set; }
    }
}
