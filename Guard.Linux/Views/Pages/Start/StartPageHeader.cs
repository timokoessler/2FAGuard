using Avalonia;
using Avalonia.Controls;

namespace Guard.Linux.Views.Pages.Start
{
    internal static class StartPageHeader
    {
        /// <summary>
        /// Hides the logo and title of a start page if the window is not high enough
        /// </summary>
        internal static void Register(Control page, Control logo, Control title, Control subtitle, double threshold = 600)
        {
            void Update(object? sender, (double width, double height) size)
            {
                bool show = size.height >= threshold;
                logo.IsVisible = show;
                title.IsVisible = show;
                subtitle.Margin = new Thickness(0, show ? 20 : 60, 0, 30);
            }

            page.Loaded += (_, _) =>
            {
                WPF.Core.EventManager.WindowSizeChanged += Update;
                var window = App.MainWindow!;
                Update(null, (window.Bounds.Width, window.Bounds.Height));
            };
            page.Unloaded += (_, _) => WPF.Core.EventManager.WindowSizeChanged -= Update;
        }
    }
}
