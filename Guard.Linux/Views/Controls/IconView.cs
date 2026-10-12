using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Guard.Core;
using Guard.Core.Models;

namespace Guard.Linux.Views.Controls
{
    internal static class IconView
    {
        /// <summary>
        /// Shows a token icon in either the svg or the image control.
        /// Returns false if a custom icon could not be loaded.
        /// </summary>
        internal static bool Show(TotpIcon icon, Avalonia.Svg.Skia.Svg svgView, Image imageView)
        {
            if (icon.Type != IconType.Custom)
            {
                imageView.IsVisible = false;
                imageView.Source = null;
                svgView.IsVisible = true;
                svgView.Source = icon.Svg;
                return true;
            }

            if (icon.Path == null || !File.Exists(icon.Path))
            {
                return false;
            }

            try
            {
                if (icon.Path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                {
                    imageView.IsVisible = false;
                    imageView.Source = null;
                    svgView.IsVisible = true;
                    svgView.Source = File.ReadAllText(icon.Path);
                }
                else
                {
                    using FileStream stream = File.OpenRead(icon.Path);
                    imageView.Source = new Bitmap(stream);
                    imageView.IsVisible = true;
                    svgView.IsVisible = false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, "Failed to decode custom icon: {Path}", icon.Path);
                return false;
            }
        }
    }
}
