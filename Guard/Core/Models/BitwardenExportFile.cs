namespace Guard.Core.Models
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
            public required string Totp { get; set; }
        }

        internal class Item
        {
            public string? Id { get; set; }
            public int? Type { get; set; }
            public string? Name { get; set; }
            public string? Notes { get; set; }
            public Login? Login { get; set; }
        }

        public bool? Encrypted { get; set; }
        public Folder[]? Folders { get; set; }
        public Item[]? Items { get; set; }
    }
}
