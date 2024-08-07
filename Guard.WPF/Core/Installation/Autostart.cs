using Guard.Core;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace Guard.WPF.Core.Installation
{
    internal class Autostart
    {
        internal static readonly string KeyName =
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        // Set in Package.appxmanifest
        private static readonly string PackagedTaskId = "2FAGuardApp";

        internal static async Task Enable()
        {
            InstallationType installationType = InstallationContext.GetInstallationType();
            if (installationType == InstallationType.MICROSOFT_STORE)
            {
                var status = await StartupTask.GetAsync(PackagedTaskId);
                switch (status.State)
                {
                    case StartupTaskState.Disabled:
                        var result = await status.RequestEnableAsync();
                        if (result != StartupTaskState.Enabled)
                        {
                            throw new Exception(
                                "Failed to enable startup task: " + result.ToString()
                            );
                        }
                        break;
                    case StartupTaskState.DisabledByPolicy:
                        throw new Exception(
                            I18n.GetString("settings.autostart.error.disabled.policy")
                        );
                    case StartupTaskState.DisabledByUser:
                        throw new Exception(
                            I18n.GetString("settings.autostart.error.disabled.user")
                        );
                    case StartupTaskState.Enabled:
                        throw new Exception(
                            I18n.GetString("i.settings.autostart.error.alreadyenabled")
                        );
                    case StartupTaskState.EnabledByPolicy:
                        throw new Exception(
                            I18n.GetString("i.settings.autostart.error.alreadyenabled")
                        );
                }
                return;
            }

            RegistryKey? rkApp =
                Registry.CurrentUser.OpenSubKey(KeyName, true)
                ?? throw new System.Exception("Failed to open registry key");
            string path =
                Environment.ProcessPath ?? throw new System.Exception("Failed to get process path");
            rkApp.SetValue("2FAGuard", $"{path} --autostart");
            rkApp.Close();
        }

        internal static async Task Disable()
        {
            InstallationType installationType = InstallationContext.GetInstallationType();
            if (installationType == InstallationType.MICROSOFT_STORE)
            {
                var status = await StartupTask.GetAsync(PackagedTaskId);
                if (
                    status.State != StartupTaskState.Enabled
                    && status.State != StartupTaskState.EnabledByPolicy
                )
                {
                    throw new Exception(I18n.GetString("i.settings.autostart.error.notenabled"));
                }
                status.Disable();
                if (status.State != StartupTaskState.Disabled)
                {
                    throw new Exception(
                        "Failed to disable startup task: " + status.State.ToString()
                    );
                }
                return;
            }
            RegistryKey? rkApp =
                Registry.CurrentUser.OpenSubKey(KeyName, true)
                ?? throw new System.Exception("Failed to open registry key");
            rkApp.DeleteValue("2FAGuard", false);
            rkApp.Close();
        }

        internal static async Task<bool> IsEnabled()
        {
            InstallationType installationType = InstallationContext.GetInstallationType();
            if (installationType == InstallationType.MICROSOFT_STORE)
            {
                var status = await StartupTask.GetAsync(PackagedTaskId);
                return status.State == StartupTaskState.Enabled
                    || status.State == StartupTaskState.EnabledByPolicy;
            }

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
