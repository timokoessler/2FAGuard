namespace Guard.WPF.Core.Models
{
    public class UpdateInfoDownloadUrls
    {
        public required string Installer { get; set; }
        public required string Portable { get; set; }
    }

    public class UpdateInfo
    {
        public required string Version { get; set; }
        public required UpdateInfoDownloadUrls Urls { get; set; }
    }
}
