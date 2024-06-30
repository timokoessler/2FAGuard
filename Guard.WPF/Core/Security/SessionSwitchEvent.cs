using Guard.Core.Security;
using Guard.Core.Storage;
using Microsoft.Win32;

namespace Guard.WPF.Core.Security
{
    internal class SessionSwitchEvent
    {
        private static MainWindow? mainWindow;

        public static void Register(MainWindow mainWindow_)
        {
            mainWindow = mainWindow_;
            SystemEvents.SessionSwitch += OnSessionSwitch;
        }

        private static void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (
                e.Reason == SessionSwitchReason.SessionLock
                && Auth.IsLoggedIn()
                && SettingsManager.Settings.LockOnScreenLock
            )
            {
                mainWindow?.Logout();
            }
        }
    }
}
