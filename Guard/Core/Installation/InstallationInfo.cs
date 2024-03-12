using Guard.Core.Models;

namespace Guard.Core.Installation
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
            return false;
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
                InstallationType.CLASSIC_INSTALLER => "Classic Installation",
                InstallationType.CLASSIC_PORTABLE => "Portable",
                InstallationType.MICROSOFT_STORE => "Microsoft Store",
                _ => "Unknown",
            };
        }

        internal static string GetVersionString()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? "????";
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
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "2FAGuard"
            );
        }
    }
}
