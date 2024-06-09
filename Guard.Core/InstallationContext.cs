
namespace Guard.Core
{
    public enum InstallationType
    {
        CLASSIC_INSTALLER,
        CLASSIC_PORTABLE,
        MICROSOFT_STORE
    }

    public static class InstallationContext
    {

        private static string? appDataFolderPath;
        private static InstallationType? installationType;
        private static bool isPortable;
        private static Version? version;

        public static void Init(InstallationType installationType, bool isPortable, Version version)
        {
            InstallationContext.installationType = installationType;
            InstallationContext.isPortable = isPortable;
            InstallationContext.version = version;

            if(isPortable)
            {
                appDataFolderPath = Path.Combine(
                    AppContext.BaseDirectory
                        ?? throw new Exception("Could not get process directory"),
                    "2FAGuard-Data"
                    );
            } else if(installationType == InstallationType.MICROSOFT_STORE)
            {
                appDataFolderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "2FAGuardStoreApp"
                );
            } else
            {
                appDataFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "2FAGuard"
            );
            }
        }

        public static string GetAppDataFolderPath()
        {
            return appDataFolderPath ?? throw new Exception("AppDataFolderPath not initialized");
        }

        public static string GetInstallationTypeString()
        {
            return GetInstallationType() switch
            {
                InstallationType.CLASSIC_INSTALLER => "Desktop Installation",
                InstallationType.CLASSIC_PORTABLE => "Portable",
                InstallationType.MICROSOFT_STORE => "Microsoft Store",
                _ => "Unknown",
            };
        }

        public static string GetVersionString()
        {
            return GetVersion().ToString() ?? "????";
        }

        public static Version GetVersion()
        {
            return version ?? throw new Exception("Version not initialized");
        }

        public static InstallationType GetInstallationType()
        {
            return installationType ?? throw new Exception("Installation type not initialized");
        }

        public static bool IsPortable()
        {
            return isPortable;
        }

    }
}
