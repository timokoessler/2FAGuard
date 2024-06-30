namespace Guard.Core.Models
{
    public class TotpIcon
    {
        public required IconType Type { get; set; }
        public required string Name { get; set; }
        public string? Svg { get; set; }
        public string? Path { get; set; }
    }
}
