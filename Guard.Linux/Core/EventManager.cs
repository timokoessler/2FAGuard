using Guard.Core.Models;

namespace Guard.WPF.Core
{
    internal class EventManager
    {
        internal class AppThemeChangedEventArgs : EventArgs
        {
            internal ThemeSetting Theme { get; set; }

            internal enum EventSource
            {
                Navigation,
                Settings,
            }

            internal EventSource Source { get; set; }
        }

        internal static event EventHandler<int> TokenDeleted = delegate { };
        internal static event EventHandler<AppThemeChangedEventArgs> AppThemeChanged = delegate { };
        internal static event EventHandler<(double, double)> WindowSizeChanged = delegate { };

        internal static void EmitTokenDeleted(int tokenID)
        {
            TokenDeleted?.Invoke(null, tokenID);
        }

        internal static void EmitAppThemeChanged(
            ThemeSetting theme,
            AppThemeChangedEventArgs.EventSource source
        )
        {
            AppThemeChanged?.Invoke(
                null,
                new AppThemeChangedEventArgs { Theme = theme, Source = source }
            );
        }

        internal static void EmitWindowSizeChanged(double width, double height)
        {
            WindowSizeChanged?.Invoke(null, (width, height));
        }
    }
}
