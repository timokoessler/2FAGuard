using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Guard.Core;
using Guard.Core.Security;
using Guard.Core.Storage;
using Guard.Linux.Core;
using Guard.Linux.Views;
using Guard.WPF.Core;
using Guard.WPF.Core.Installation;

namespace Guard.Linux
{
    public partial class App : Application
    {
        // Increase the minimum app version when making breaking changes to prevent downgrades
        private static readonly Version minimumCompatibleVersion = new(1, 7, 0);

        private FileStream? singleInstanceLock;
        private bool isQuitting;

        internal static MainWindow? MainWindow { get; private set; }

        internal static App Instance => (App)Current!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                desktop.Exit += (_, _) => singleInstanceLock?.Dispose();
                _ = Startup(desktop);
            }
            base.OnFrameworkInitializationCompleted();
        }

        private async Task Startup(IClassicDesktopStyleApplicationLifetime desktop)
        {
            var installationInfo = InstallationInfo.GetInstallationContext();
            InstallationContext.Init(installationInfo.installationType, installationInfo.version);

            bool notAlreadyRunning = AcquireSingleInstanceLock();

            MainWindow = new MainWindow(desktop.Args?.Contains("--autostart") == true);
            desktop.MainWindow = MainWindow;

            (bool appDataFolderOk, string? appDataFolderError) =
                InstallationContext.CheckAppDataFolder();
            if (!appDataFolderOk)
            {
                MainWindow.Show();
                await Dialogs.MessageBox(
                    "Error checking app data folder",
                    appDataFolderError ?? "",
                    closeButtonText: "Exit"
                );
                Exit(desktop, 1);
                return;
            }

            Log.Init();
            SettingsManager.Init();
            I18n.Init();
            ExceptionHandler.Register();

            if (!notAlreadyRunning)
            {
                // Another instance is already running, try to bring it to the front
                if (!IPC.SendToFront())
                {
                    MainWindow.Show();
                    await Dialogs.MessageBox(
                        I18n.GetString("i.running.title"),
                        I18n.GetString("i.running.content"),
                        closeButtonText: I18n.GetString("i.running.exit")
                    );
                }
                Exit(desktop, 0);
                return;
            }

            if (!EncryptionHelper.IsSupported())
            {
                MainWindow.Show();
                await Dialogs.MessageBox(
                    I18n.GetString("i.unsupported.vcpp.title").Replace("Visual C++ Redistributable", "libsodium"),
                    "The bundled libsodium library could not be loaded. Please reinstall 2FAGuard.",
                    closeButtonText: I18n.GetString("i.unsupported.exit")
                );
                Exit(desktop, 1);
                return;
            }

            if (!await CheckForUnsupportedVersion(installationInfo.version))
            {
                Exit(desktop, 1);
                return;
            }

            MainWindow.Start();
        }

        // Shutdown must not be called before the main loop is running
        private static void Exit(IClassicDesktopStyleApplicationLifetime desktop, int exitCode)
        {
            Dispatcher.UIThread.Post(() => desktop.Shutdown(exitCode));
        }

        private bool AcquireSingleInstanceLock()
        {
            try
            {
                singleInstanceLock = new FileStream(
                    InstallationContext.GetPipeName("Instance") + ".lock",
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None
                );
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>
        /// Exits the app (also when minimize to tray is enabled)
        /// </summary>
        internal async void Quit()
        {
            if (isQuitting)
            {
                return;
            }
            isQuitting = true;
            try
            {
                await ClipboardClearService.ClearNow();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Error during shutdown: {0}", ex.Message);
            }
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }

        internal bool IsQuitting => isQuitting;

        /// <summary>
        /// Checks if the current app version is supported based on the minimum app version stored in settings.
        /// The minimum app version prevents older versions from running after a newer version with breaking changes.
        /// </summary>
        private static async Task<bool> CheckForUnsupportedVersion(Version currentVersion)
        {
            bool settingsChanged = false;

            if (
                SettingsManager.Settings.MinimumAppVersion.Major == 0
                && SettingsManager.Settings.MinimumAppVersion.Minor == 0
            )
            {
                SettingsManager.Settings.MinimumAppVersion = minimumCompatibleVersion;
                settingsChanged = true;
            }

            if (minimumCompatibleVersion > SettingsManager.Settings.MinimumAppVersion)
            {
                SettingsManager.Settings.MinimumAppVersion = minimumCompatibleVersion;
                settingsChanged = true;
            }

            bool isCompatible = currentVersion >= SettingsManager.Settings.MinimumAppVersion;

            if (isCompatible && SettingsManager.Settings.LastUsedAppVersion != currentVersion)
            {
                SettingsManager.Settings.LastUsedAppVersion = currentVersion;
                settingsChanged = true;
            }

            if (settingsChanged)
            {
                await SettingsManager.Save();
            }

            if (!isCompatible)
            {
                Log.Logger.Error(
                    "Preventing app start: Current version is older than the minimum version: {0} < {1}",
                    currentVersion,
                    SettingsManager.Settings.MinimumAppVersion
                );
                MainWindow!.Show();
                await Dialogs.MessageBox(
                    I18n.GetString("i.unsupported.olderversion.title"),
                    I18n.GetString("i.unsupported.olderversion.content"),
                    closeButtonText: I18n.GetString("i.unsupported.exit")
                );
            }

            return isCompatible;
        }
    }
}
