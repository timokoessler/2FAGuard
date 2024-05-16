using System.IO;
using Guard.WPF.Core.Installation;

namespace Guard.WPF.Core.Icons
{
    class IconManager
    {
        internal enum IconType
        {
            Default,
            Any,
            SimpleIcons,
            Custom
        }

        internal class TotpIcon
        {
            public required IconType Type { get; set; }
            public required string Name { get; set; }
            public string? Svg { get; set; }
            public string? Path { get; set; }
        }

        private static readonly string customIconsPath = Path.Combine(
            InstallationInfo.GetAppDataFolderPath(),
            "icons"
        );

        public static void LoadIcons()
        {
            Task.Run(() => SimpleIconsManager.LoadIcons());
        }

        public static string[] GetIconNames()
        {
            return SimpleIconsManager.GetIconNames();
        }

        private static readonly string defaultIconSVG =
            "<svg id=\"Layer_2\" enable-background=\"new 0 0 100 100\" viewBox=\"0 0 100 100\" xmlns=\"http://www.w3.org/2000/svg\"><linearGradient id=\"SVGID_1_\" gradientUnits=\"userSpaceOnUse\" x1=\"-9.237\" x2=\"99.736\" y1=\"-.469\" y2=\"107.441\"><stop offset=\".1507538\" stop-color=\"#1da3ff\"/><stop offset=\".8542714\" stop-color=\"#0048c6\"/></linearGradient><path d=\"m89.1815948 10.7360106c-10.9813919-10.9813921-28.7712554-10.9813251-39.7526474.0000667-7.5442123 7.5442114-9.9051704 18.3169556-7.0720215 27.8926964l-39.7745678 39.7745695v19.0856628l18.7671616.010994-.6588573-11.9917297 10.9155064.5931091-.5930386-10.9155731 10.904583.6040268-.5930405-10.9155731 11.9916611.6589203 7.9725037-7.972496c9.5758095 2.8332176 20.3485527.4722557 27.8927574-7.0719528 10.9813919-10.981392 10.9813919-28.7713281-.0000001-39.7527209zm-5.6883468 5.6883535c3.4591599 3.4591656 3.4591599 9.0816326 0 12.5407982-3.4591675 3.4591656-9.0816345 3.4591656-12.540802 0-3.4590988-3.4590988-3.4590988-9.0815659.0000687-12.5407314s9.0816345-3.4591656 12.5407333-.0000668z\" fill=\"url(#SVGID_1_)\"/></svg>";

        private static readonly TotpIcon defaultIcon =
            new()
            {
                Type = IconType.Default,
                Svg = defaultIconSVG,
                Name = "default",
            };

        public static TotpIcon GetIcon(string name, IconType type)
        {
            if (name == null || name == "default" || type == IconType.Default)
            {
                return defaultIcon;
            }

            if (type == IconType.SimpleIcons || type == IconType.Any)
            {
                TotpIcon? simpleIcon = SimpleIconsManager.GetTotpIcon(name);
                return simpleIcon ?? defaultIcon;
            }

            if (type == IconType.Custom)
            {
                return new TotpIcon
                {
                    Type = IconType.Custom,
                    Name = name,
                    Path = Path.Combine(customIconsPath, name),
                };
            }

            return defaultIcon;
        }

        public static string GetLicense(TotpIcon icon)
        {
            if (icon.Type == IconType.SimpleIcons)
            {
                string? siLicense = SimpleIconsManager.GetLicense();
                return string.IsNullOrEmpty(siLicense) ? string.Empty : siLicense;
            }
            return string.Empty;
        }

        public static async Task<string> AddCustomIcon(string path)
        {
            if (!Directory.Exists(customIconsPath))
            {
                Directory.CreateDirectory(customIconsPath);
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("The source file does not exist.");
            }

            if (new FileInfo(path).Length > 1000000)
            {
                throw new Exception(I18n.GetString("i.td.customicon.tolarge"));
            }

            string id = Guid.NewGuid().ToString();
            string ext =
                Path.GetExtension(path)
                ?? throw new Exception("The file extension could not be determined.");
            string[] allowedExtensions = [".svg", ".png", ".jpg", ".jpeg"];
            if (!allowedExtensions.Contains(ext))
            {
                throw new Exception("The file extension is not allowed.");
            }

            string destPath = Path.Combine(customIconsPath, $"{id}{ext}");

            await Task.Run(() => File.Copy(path, destPath));

            return $"{id}{ext}";
        }

        public static void RemoveCustomIcon(string name)
        {
            string path = Path.Combine(customIconsPath, name);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static void RemoveAllCustomIcons()
        {
            if (Directory.Exists(customIconsPath))
            {
                Directory.Delete(customIconsPath, true);
            }
        }
    }
}
