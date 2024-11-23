using System.Security.Cryptography;
using System.Text;
using Guard.Core.Security;

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
        private static Version? version;

        public static void Init(InstallationType installationType, Version version)
        {
            InstallationContext.installationType = installationType;
            InstallationContext.version = version;

            if (IsPortable())
            {
                appDataFolderPath = Path.Combine(
                    AppContext.BaseDirectory
                        ?? throw new Exception("Could not get process directory"),
                    "2FAGuard-Data"
                );
            }
            else if (installationType == InstallationType.MICROSOFT_STORE)
            {
                appDataFolderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "2FAGuardStoreApp"
                );
            }
            else
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
            return installationType == InstallationType.CLASSIC_PORTABLE;
        }

        public static int GetOsBuildVersion()
        {
            return Environment.OSVersion.Version.Build;
        }

        public static string GetLocaleCode()
        {
            return System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }

        public static string GetOsVersionString()
        {
            if (Environment.OSVersion.Version.Build >= 22000)
            {
                return $"Windows 11 {Environment.OSVersion.Version.Build}";
            }
            return $"Windows 10 {Environment.OSVersion.Version.Build}";
        }

        public static string GetAppEditionString()
        {
            return GetInstallationType() switch
            {
                InstallationType.CLASSIC_PORTABLE => "portable",
                InstallationType.CLASSIC_INSTALLER => "installer",
                InstallationType.MICROSOFT_STORE => "store",
                _ => "unknown"
            };
        }

        public static string GetMutexName()
        {
            string prefix = "2FAGuard-";
            // Used for Mutex to allow multiple users on the same machine or multiple portable installations to run at the same time
            string hashContent = Environment.UserDomainName + Environment.UserName;

            if (installationType == InstallationType.CLASSIC_PORTABLE)
            {
                prefix = "2FAGuardPortable-";
                hashContent += AppContext.BaseDirectory;
            }
            else if (installationType == InstallationType.MICROSOFT_STORE)
            {
                prefix = "2FAGuardStore-";
            }

            return prefix
                + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hashContent)));
        }
    }
}
