using Microsoft.Win32;

namespace Guard.Core.Installation
{
    internal class Autostart
    {
        internal static readonly string KeyName =
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        internal static void Enable()
        {
            RegistryKey? rkApp =
                Registry.CurrentUser.OpenSubKey(KeyName, true)
                ?? throw new System.Exception("Failed to open registry key");
            string path =
                Environment.ProcessPath ?? throw new System.Exception("Failed to get process path");
            rkApp.SetValue("2FAGuard", path);
            rkApp.Close();
        }

        internal static void Disable()
        {
            RegistryKey? rkApp =
                Registry.CurrentUser.OpenSubKey(KeyName, true)
                ?? throw new System.Exception("Failed to open registry key");
            rkApp.DeleteValue("2FAGuard", false);
            rkApp.Close();
        }

        internal static bool IsEnabled()
        {
            RegistryKey? rkApp = Registry.CurrentUser.OpenSubKey(KeyName, true);
            if (rkApp == null)
            {
                return false;
            }
            var value = rkApp.GetValue("2FAGuard");
            rkApp.Close();
            return value != null;
        }
    }
}
