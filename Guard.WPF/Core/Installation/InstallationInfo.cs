using Guard.WPF.Core.Models;

namespace Guard.WPF.Core.Installation
{
    internal enum InstallationType
    {
        CLASSIC_INSTALLER,
        CLASSIC_PORTABLE,
        MICROSOFT_STORE
    }

    class InstallationInfo
    {
        internal static bool IsPortable()
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

        internal static InstallationType GetInstallationType()
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

        internal static string GetInstallationTypeString()
        {
            return GetInstallationType() switch
            {
                InstallationType.CLASSIC_INSTALLER => "Desktop Installation",
                InstallationType.CLASSIC_PORTABLE => "Portable",
                InstallationType.MICROSOFT_STORE => "Microsoft Store",
                _ => "Unknown",
            };
        }

        internal static string GetVersionString()
        {
            return GetVersion().ToString() ?? "????";
        }

        internal static Version GetVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
                ?? new Version(0, 0);
        }

        internal static string GetAppDataFolderPath()
        {
            if (IsPortable())
            {
                return System.IO.Path.Combine(
                    AppContext.BaseDirectory
                        ?? throw new Exception("Could not get process directory"),
                    "2FAGuard-Data"
                );
            }
            if (GetInstallationType() == InstallationType.MICROSOFT_STORE)
            {
                return System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "2FAGuardStoreApp"
                );
            }
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "2FAGuard"
            );
        }
    }
}
