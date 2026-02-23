using System.Reflection;
using System.Text.Json;
using Guard.Core.Models;

namespace Guard.WPF.Core.Icons
{
    public class SimpleIcon
    {
        /// <summary>
        /// Title
        /// </summary>
        public required string T { get; set; }

        /// <summary>
        /// The svg path
        /// </summary>
        public required string P { get; set; }

        /// <summary>
        /// The hex color
        /// </summary>
        public required string H { get; set; }
    }

    class SimpleIconsManager
    {
        private static Dictionary<string, SimpleIcon>? iconsByName;
        private static string? license;

        private class SimpleIconJSON
        {
            public required string Version { get; set; }
            public required SimpleIcon[] Icons { get; set; }
            public required string License { get; set; }
        }

        public static void LoadIcons()
        {
            using var stream =
                Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceStream("Guard.WPF.Assets.Icons.si.json")
                ?? throw new Exception("Can not find internal SimpleIcon JSON file");
            using var reader = new System.IO.StreamReader(stream);
            var json = reader.ReadToEnd();
            var parsedJson =
                JsonSerializer.Deserialize<SimpleIconJSON>(json)
                ?? throw new Exception("Can not parse internal SimpleIcon JSON file");
            iconsByName = parsedJson.Icons.ToDictionary(icon => icon.T);
            license = parsedJson.License;
        }

        public static string[] GetIconNames() =>
            iconsByName?.Keys.ToArray() ?? throw new InvalidOperationException("Icons not loaded");

        public static SimpleIcon? GetSimpleIcon(string name)
        {
            return iconsByName?.GetValueOrDefault(name);
        }

        public static TotpIcon? GetTotpIcon(string name)
        {
            SimpleIcon? simpleIcon = GetSimpleIcon(name);
            if (simpleIcon == null)
            {
                return null;
            }

            string hexColor = IconColor.GetIconColor(simpleIcon.H);

            return new TotpIcon
            {
                Type = IconType.SimpleIcons,
                Svg =
                    $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"{hexColor}\"><path d=\"{simpleIcon.P}\"/></svg>",
                Name = name,
            };
        }

        public static string? GetLicense()
        {
            return license;
        }
    }
}
