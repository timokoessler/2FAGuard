using Guard.Core;

namespace Guard.WPF.Core.Installation
{
    public class InstallationInfo
    {
        private static bool IsPortable()
        {
#if PORTABLE
            return true;
#else
            // A "2FAGuard-Data" folder next to the binary also enables portable mode
            return Directory.Exists(Path.Combine(AppContext.BaseDirectory, "2FAGuard-Data"));
#endif
        }

        private static Version GetVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
                ?? new Version(0, 0);
        }

        public static (InstallationType installationType, Version version) GetInstallationContext()
        {
            return (
                IsPortable() ? InstallationType.CLASSIC_PORTABLE : InstallationType.CLASSIC_INSTALLER,
                GetVersion()
            );
        }

        public static bool IsInDebugMode()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}
