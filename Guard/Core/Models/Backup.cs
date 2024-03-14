namespace Guard.Core.Models
{
    internal class Backup
    {
        public int Version { get; set; }
        public required DBTOTPToken[] Tokens { get; set; }
    }
}
