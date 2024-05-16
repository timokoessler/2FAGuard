namespace Guard.WPF.Core.Models
{
    internal class BitwardenExportFile
    {
        internal class Folder
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
        }

        internal class Login
        {
            public string? Username { get; set; }
            public string? Totp { get; set; }
        }

        internal class Item
        {
            public string? Id { get; set; }
            public int? Type { get; set; }
            public string? Name { get; set; }
            public string? Notes { get; set; }
            public Login? Login { get; set; }
        }

        internal enum BWKdfType
        {
            Pbkdf2 = 0,
            Argon2id = 1
        }

        public bool? Encrypted { get; set; }
        public Folder[]? Folders { get; set; }
        public Item[]? Items { get; set; }
        public bool? PasswordProtected { get; set; }
        public string? Salt { get; set; }
        public BWKdfType? KdfType { get; set; }
        public int? KdfIterations { get; set; }
        public int? KdfMemory { get; set; }
        public int? KdfParallelism { get; set; }
        public string? EncKeyValidation_DO_NOT_EDIT { get; set; }
        public string? Data { get; set; }
    }
}
