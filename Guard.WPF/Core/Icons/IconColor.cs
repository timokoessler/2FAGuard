using System.Globalization;
using Wpf.Ui.Appearance;

namespace Guard.WPF.Core.Icons
{
    internal static class IconColor
    {
        const string DefaultLightModeColor = "#313131";
        const string DefaultDarkModeColor = "#ffffff";

        public static string GetIconColor(string? iconColor)
        {
            bool isDarkMode = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;
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
            // Remove the '#' character from the hex color
            hexColor = hexColor.Replace("#", "");

            // Convert the hex color to RGB components
            int r = int.Parse(hexColor[..2], NumberStyles.HexNumber);
            int g = int.Parse(hexColor.Substring(2, 2), NumberStyles.HexNumber);
            int b = int.Parse(hexColor.Substring(4, 2), NumberStyles.HexNumber);

            // Calculate luminance of the given color
            double luminance = CalculateLuminance(r, g, b);

            // Define luminance thresholds for dark and light backgrounds
            double backgroundLuminance = !isDarkMode ? 1.0 : 0.0;
            double threshold = 0.1;

            // Check if the color's luminance is too close to the background
            return Math.Abs(luminance - backgroundLuminance) < threshold;
        }

        private static double CalculateLuminance(int r, int g, int b)
        {
            // Convert RGB to relative luminance based on WCAG formula
            // https://www.w3.org/TR/WCAG20/#relativeluminancedef
            double R =
                (r / 255.0 <= 0.03928)
                    ? r / 255.0 / 12.92
                    : Math.Pow((r / 255.0 + 0.055) / 1.055, 2.4);
            double G =
                (g / 255.0 <= 0.03928)
                    ? g / 255.0 / 12.92
                    : Math.Pow((g / 255.0 + 0.055) / 1.055, 2.4);
            double B =
                (b / 255.0 <= 0.03928)
                    ? b / 255.0 / 12.92
                    : Math.Pow((b / 255.0 + 0.055) / 1.055, 2.4);

            // Calculate luminance
            return 0.2126 * R + 0.7152 * G + 0.0722 * B;
        }
    }
}
