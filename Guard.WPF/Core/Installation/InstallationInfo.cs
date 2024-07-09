using Guard.Core;

namespace Guard.WPF.Core.Installation
{
    public class InstallationInfo
    {
        private static bool IsPortable()
        {
#if PORTABLE
            return true;
#endif
#pragma warning disable IDE0079 // Unnötige Unterdrückung entfernen
#pragma warning disable CS0162
            return false;
#pragma warning restore CS0162
#pragma warning restore IDE0079
        }

        private static InstallationType GetInstallationType()
        {
            if (IsPortable())
            {
                return InstallationType.CLASSIC_PORTABLE;
            }
            else if (DesktopBridgeHelper.IsRunningAsUwp())
            {
                return InstallationType.MICROSOFT_STORE;
            }
            else
            {
                return InstallationType.CLASSIC_INSTALLER;
            }
        }

        private static Version GetVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
                ?? new Version(0, 0);
        }

        public static (InstallationType installationType, Version version) GetInstallationContext()
        {
            return (GetInstallationType(), GetVersion());
        }
    }
}
