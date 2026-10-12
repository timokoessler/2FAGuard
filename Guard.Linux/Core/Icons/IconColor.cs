using System.Globalization;
using Avalonia;
using Avalonia.Styling;

namespace Guard.WPF.Core.Icons
{
    internal static class IconColor
    {
        const string DefaultLightModeColor = "#313131";
        const string DefaultDarkModeColor = "#ffffff";

        public static bool IsDarkMode()
        {
            return Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        }

        public static string GetIconColor(string? iconColor)
        {
            bool isDarkMode = IsDarkMode();
            if (iconColor == null)
            {
                return isDarkMode ? DefaultDarkModeColor : DefaultLightModeColor;
            }

            if (IsColorTooSimilarToBackground(iconColor, isDarkMode))
            {
                return isDarkMode ? DefaultDarkModeColor : DefaultLightModeColor;
            }

            if (!iconColor.StartsWith('#'))
            {
                return $"#{iconColor}";
            }

            return iconColor;
        }

        private static bool IsColorTooSimilarToBackground(string hexColor, bool isDarkMode)
        {
            hexColor = hexColor.Replace("#", "");

            int r = int.Parse(hexColor[..2], NumberStyles.HexNumber);
            int g = int.Parse(hexColor.Substring(2, 2), NumberStyles.HexNumber);
            int b = int.Parse(hexColor.Substring(4, 2), NumberStyles.HexNumber);

            double luminance = CalculateLuminance(r, g, b);
            double backgroundLuminance = !isDarkMode ? 1.0 : 0.0;
            double threshold = 0.1;

            return Math.Abs(luminance - backgroundLuminance) < threshold;
        }

        // https://www.w3.org/TR/WCAG20/#relativeluminancedef
        private static double CalculateLuminance(int r, int g, int b)
        {
            static double Channel(int c) =>
                (c / 255.0 <= 0.03928) ? c / 255.0 / 12.92 : Math.Pow((c / 255.0 + 0.055) / 1.055, 2.4);

            return 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);
        }
    }
}
