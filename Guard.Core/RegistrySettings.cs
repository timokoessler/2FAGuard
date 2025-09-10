using Microsoft.Win32;

namespace Guard.Core
{
    public class RegistrySettings
    {
        private static T? GetValue<T>(string keyPath, string value, T? defaultValue)
        {
            try
            {
                return (T?)Registry.GetValue(keyPath, value, defaultValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        private static bool GetValue(string keyPath, string value, bool defaultValue)
        {
            try
            {
                int? val = (int?)Registry.GetValue(keyPath, value, defaultValue ? 1 : 0);
                if (val == null)
                {
                    return defaultValue;
                }
                return val != 0;
            }
            catch
            {
                return defaultValue;
            }
        }

        public static string? GetAppDataPath()
        {
            string? path = GetValue<string>(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard",
                "AppDataPath",
                null
            );
            if (path != null)
            {
                path = Environment.ExpandEnvironmentVariables(path);
            }
            return path;
        }

        public static (bool hideSkip, bool hideWinHello, bool hidePassword) GetSetupHideOptions()
        {
            bool hideSkip = GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard\Setup",
                "HideSkip",
                false
            );
            bool hideWinHello = GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard\Setup",
                "HideWinHello",
                false
            );
            bool hidePassword = GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard\Setup",
                "HidePassword",
                false
            );

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
            bool hideWinHello = GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard\Settings",
                "HideWinHello",
                false
            );
            bool hidePreventRecording = GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard\Settings",
                "HidePreventRecording",
                false
            );
            bool hideSecurityKey = GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard\Settings",
                "HideSecurityKey",
                false
            );

            return (hideWinHello, hidePreventRecording, hideSecurityKey);
        }

        public static (
            bool requireLowerAndUpperCase,
            bool requireDigits,
            bool requireSpecialChars,
            int minLength
        ) GetPasswordOptions()
        {
            bool requireLowerAndUpperCase = GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard\Password",
                "RequireLowerAndUpperCase",
                false
            );
            bool requireDigits = GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard\Password",
                "RequireDigits",
                false
            );
            bool requireSpecialChars = GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard\Password",
                "RequireSpecialChars",
                false
            );
            int minLength = GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard\Password",
                "MinLength",
                8
            );
            return (requireLowerAndUpperCase, requireDigits, requireSpecialChars, minLength);
        }

        public static bool PreventUnencryptedExports()
        {
            return GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard",
                "PreventUnencryptedExports",
                false
            );
        }

        public static bool DisableAutoUpdate()
        {
            return GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard",
                "DisableAutoUpdate",
                false
            );
        }

        public static bool DisableStats()
        {
            return GetValue(@"HKEY_CURRENT_USER\Software\Policies\2FAGuard", "DisableStats", false);
        }

        public static bool DisableScreenRecordingProtection()
        {
            return GetValue(
                @"HKEY_CURRENT_USER\Software\Policies\2FAGuard",
                "DisableScreenRecordingProtection",
                false
            );
        }
    }
}
