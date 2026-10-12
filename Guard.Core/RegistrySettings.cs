using Guard.Core.Models;
using Guard.Core.Platform;

namespace Guard.Core
{
    public static class RegistrySettings
    {
        private static ISettingsProvider? _provider;
        private static readonly object _lock = new();
        
        private static ISettingsProvider Provider
        {
            get
            {
                if (_provider == null)
                {
                    lock (_lock)
                    {
                        _provider ??= new JsonSettingsProvider(
                            Path.Combine(InstallationContext.GetAppDataFolderPath(), "settings.json"));
                    }
                }
                return _provider;
            }
        }

        private static T? GetValue<T>(string key, T? defaultValue)
        {
            return Provider.GetValue(key, defaultValue);
        }

        private static bool GetValue(string key, bool defaultValue)
        {
            return Provider.GetValue(key, defaultValue);
        }

        public static string? GetAppDataPath()
        {
            return GetValue<string>("AppDataPath", null);
        }

        public static (bool hideSkip, bool hideWinHello, bool hidePassword) GetSetupHideOptions()
        {
            bool hideSkip = GetValue("Setup.HideSkip", false);
            bool hideWinHello = GetValue("Setup.HideWinHello", false);
            bool hidePassword = GetValue("Setup.HidePassword", false);

            // If all options are hidden, show them all
            if (hideSkip && hideWinHello && hidePassword)
            {
                return (false, false, false);
            }

            return (hideSkip, hideWinHello, hidePassword);
        }

        public static (
            bool hideWinHello,
            bool hidePreventScreenRecording,
            bool hideSecurityKey
        ) GetSettingsPageOptions()
        {
            bool hideWinHello = GetValue("Settings.HideWinHello", false);
            bool hidePreventRecording = GetValue("Settings.HidePreventRecording", false);
            bool hideSecurityKey = GetValue("Settings.HideSecurityKey", false);

            return (hideWinHello, hidePreventRecording, hideSecurityKey);
        }

        public static (
            bool requireLowerAndUpperCase,
            bool requireDigits,
            bool requireSpecialChars,
            int minLength
        ) GetPasswordOptions()
        {
            bool requireLowerAndUpperCase = GetValue("Password.RequireLowerAndUpperCase", false);
            bool requireDigits = GetValue("Password.RequireDigits", false);
            bool requireSpecialChars = GetValue("Password.RequireSpecialChars", false);
            int minLength = GetValue("Password.MinLength", 8);
            return (requireLowerAndUpperCase, requireDigits, requireSpecialChars, minLength);
        }

        public static bool PreventUnencryptedExports()
        {
            return GetValue("PreventUnencryptedExports", false);
        }

        public static bool DisableAutoUpdate()
        {
            return GetValue("DisableAutoUpdate", false);
        }

        public static bool DisableStats()
        {
            return GetValue("DisableStats", false);
        }

        public static bool IsCliDesktopBridgeEnabled()
        {
            return GetValue("CLI.EnableDesktopBridge", false);
        }

        public static bool DisableScreenRecordingProtection()
        {
            return GetValue("DisableScreenRecordingProtection", false);
        }

        public static bool ForceLockOnScreenLock()
        {
            return GetValue("Settings.ForceLockOnScreenLock", false);
        }

        public static LockTimeSetting? GetForcedLockTime()
        {
            int rawValue = GetValue("Settings.ForceLockTimeSetting", -1);

            if (rawValue < (int)LockTimeSetting.Never || rawValue > (int)LockTimeSetting.OneHour)
            {
                return null;
            }

            return (LockTimeSetting)rawValue;
        }

        public static ClearClipboardSetting? GetForcedClearClipboard()
        {
            int rawValue = GetValue("Settings.ForceClearClipboardSetting", -1);

            if (
                rawValue < (int)ClearClipboardSetting.Disabled
                || rawValue > (int)ClearClipboardSetting.OneMinute
            )
            {
                return null;
            }

            return (ClearClipboardSetting)rawValue;
        }
    }
}