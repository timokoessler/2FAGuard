namespace Guard.WPF.Core.Installation
{
    /// <summary>
    /// Autostart using the XDG Autostart specification (~/.config/autostart)
    /// </summary>
    internal class Autostart
    {
        private static string GetDesktopFilePath()
        {
            string configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? "";
            if (string.IsNullOrEmpty(configHome) || !Path.IsPathRooted(configHome))
            {
                configHome = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".config"
                );
            }
            return Path.Combine(configHome, "autostart", "2fa-guard.desktop");
        }

        private static string QuoteExec(string path)
        {
            // Desktop Entry Specification: Quoting
            string escaped = path.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("`", "\\`")
                .Replace("$", "\\$");
            return $"\"{escaped}\"";
        }

        internal static async Task Enable()
        {
            string path =
                Environment.ProcessPath ?? throw new Exception("Failed to get process path");
            string desktopFile = GetDesktopFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(desktopFile)!);
            string content =
                "[Desktop Entry]\n"
                + "Type=Application\n"
                + "Name=2FAGuard\n"
                + "Comment=TOTP Authenticator\n"
                + $"Exec={QuoteExec(path)} --autostart\n"
                + "Icon=2fa-guard\n"
                + "Terminal=false\n"
                + "X-GNOME-Autostart-enabled=true\n";
            await File.WriteAllTextAsync(desktopFile, content);
        }

        internal static Task Disable()
        {
            string desktopFile = GetDesktopFilePath();
            if (File.Exists(desktopFile))
            {
                File.Delete(desktopFile);
            }
            return Task.CompletedTask;
        }

        internal static Task<bool> IsEnabled()
        {
            return Task.FromResult(File.Exists(GetDesktopFilePath()));
        }
    }
}
