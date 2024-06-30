using System.Runtime.InteropServices;
using Guard.Core;
using Guard.Core.Security;
using Guard.Core.Storage;
using Spectre.Console;

namespace Guard.CLI.Core
{
    internal static class Lifecycle
    {
        private static Mutex? singleInstanceMutex;

        public static async Task Init()
        {
            InstallationType installationType = InstallationContext.GetInstallationType();
            string mutexName = "2FAGuard";
            if (installationType == InstallationType.CLASSIC_PORTABLE)
            {
                mutexName += "Portable";
            }
            else if (installationType == InstallationType.MICROSOFT_STORE)
            {
                mutexName += "Store";
            }

            singleInstanceMutex = new Mutex(true, mutexName, out bool createdNewMutex);

            Console.CancelKeyPress += (sender, args) =>
            {
                onExit();
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                onExit();
            };

            if (!createdNewMutex)
            {
                AnsiConsole.MarkupLine(
                    "[red]Error:[/] Cannot start 2FAGuard because another instance is already running. Close other instances of 2FAGuard CLI and GUI and try again."
                );
                Environment.Exit(1);
            }

            if (Environment.OSVersion.Version.Build < 18362)
            {
                AnsiConsole.MarkupLine(
                    "[red]Error:[/] 2FAGuard requires Windows 10 version 1903 (build 18362) or later. Please update your system and try again."
                );
                Environment.Exit(1);
            }

            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                AnsiConsole.MarkupLine(
                    "[red]Error:[/] 2FAGuard requires a 64-bit processor architecture and operating system."
                );
                Environment.Exit(1);
            }

            if (!EncryptionHelper.IsSupported())
            {
                AnsiConsole.MarkupLine(
                    "[red]Error:[/] The Microsoft Visual C++ Redistributable 2015-2022 is required to run 2FAGuard. Please install the package to be able to use 2FAGuard. If you have used the 2FAGuard installer or have just installed the package, you may need to restart your device. You can download the redistributable [link=https://aka.ms/vs/17/release/vc_redist.x64.exe]here[/]."
                );
                Environment.Exit(1);
            }

            Version currentVersion = InstallationContext.GetVersion();

            if (
                SettingsManager.Settings.LastUsedAppVersion.Major == 0
                && SettingsManager.Settings.LastUsedAppVersion.Minor == 0
            )
            {
                SettingsManager.Settings.LastUsedAppVersion = currentVersion;
                await SettingsManager.Save();
            }
            int versionCompare = SettingsManager.Settings.LastUsedAppVersion.CompareTo(
                currentVersion
            );
            if (versionCompare < 0)
            {
                SettingsManager.Settings.LastUsedAppVersion = currentVersion;
                await SettingsManager.Save();
            }
            else if (versionCompare > 0)
            {
                Log.Logger.Error(
                    "Preventing app start: Current version is older than last used version: {0} < {1}",
                    currentVersion,
                    SettingsManager.Settings.LastUsedAppVersion
                );
                AnsiConsole.MarkupLine(
                    "[red]Error:[/] 2FAGuard cannot start because the app data was opened with a newer version of 2FAGuard. Please update 2FAGuard to the latest version and try again."
                );
                Environment.Exit(1);
            }

            _ = Task.Run(async () =>
            {
                TimeSpan offset = await Time.GetLocalUTCTimeOffset();
                if (offset.TotalSeconds > 10 || offset.TotalSeconds < -10)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]Warning:[/] Your system clock is out of sync by {offset.TotalSeconds} seconds. This may cause issues with time-based two-factor authentication codes."
                    );
                }
            });

            SettingsManager.Init();
            await Auth.Init();

            await CLIAuth.Login();
        }

        private static void onExit()
        {
            singleInstanceMutex?.Dispose();
        }
    }
}
