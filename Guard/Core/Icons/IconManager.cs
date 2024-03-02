﻿namespace Guard.Core.Icons
{
    class IconManager
    {
        public enum IconColor
        {
            Colored,
            Dark,
            Light
        }

        internal enum IconType
        {
            Default,
            Any,
            SimpleIcons,
        }

        internal class TotpIcon
        {
            public required IconType Type { get; set; }
            public required string Svg { get; set; }
            public required string Name { get; set; }
        }

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

        private static readonly TotpIcon defaultIcon = new TotpIcon
        {
            Type = IconType.Default,
            Svg = defaultIconSVG,
            Name = "default",
        };

        public static TotpIcon GetIcon(string name, IconColor color, IconType type)
        {
            if (name == null || name == "default" || type == IconType.Default)
            {
                return defaultIcon;
            }
            SimpleIcon? simpleIcon = SimpleIconsManager.GetIcon(name);

            if (simpleIcon == null)
            {
                return defaultIcon;
            }

            string hexColor;

            if (color == IconColor.Colored && simpleIcon.H != null)
            {
                hexColor = $"#{simpleIcon.H}";
            }
            else if (color == IconColor.Dark)
            {
                hexColor = "#313131";
            }
            else
            {
                hexColor = "#ffffff";
            }

            return new TotpIcon
            {
                Type = IconType.SimpleIcons,
                Svg =
                    $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\" fill=\"{hexColor}\"><path d=\"{simpleIcon.P}\"/></svg>",
                Name = name,
            };
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
    }
}