using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Guard.Core;
using Guard.Core.Security;
using Guard.Core.Storage;
using Guard.WPF.Core;
using Guard.WPF.Core.Installation;
using Guard.WPF.Core.Security;
using Windows.ApplicationModel.Activation;

namespace Guard.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex? singleInstanceMutex;
        private bool autostart = false;

        // Increase the minimum app version when making breaking changes to prevent downgrades
        private static readonly Version minimumCompatibleVersion = new(1, 7, 0);

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var installationInfo = InstallationInfo.GetInstallationContext();
            InstallationContext.Init(installationInfo.installationType, installationInfo.version);

            string mutexName = InstallationContext.GetMutexName();

            singleInstanceMutex = new Mutex(true, mutexName, out bool notAlreadyRunning);

            (bool appDataFolderOk, string? appDataFolderError) =
                InstallationContext.CheckAppDataFolder();
            if (!appDataFolderOk)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Error checking app data folder",
                    Content = appDataFolderError,
                    CloseButtonText = "Exit",
                };
                await uiMessageBox.ShowDialogAsync();
                Shutdown();
                return;
            }

            Log.Init();
            SettingsManager.Init();
            I18n.Init();

            if (!notAlreadyRunning)
            {
                // Another instance is already running
                // Try to bring it to the front
                bool focusedOtherProcess = false;

                try
                {
                    Process currentProcess = Process.GetCurrentProcess();
                    if (currentProcess.MainModule != null)
                    {
                        foreach (
                            Process process in Process.GetProcessesByName(
                                currentProcess.ProcessName
                            )
                        )
                        {
                            if (
                                process.Id != currentProcess.Id
                                && process.MainModule != null
                                && process.MainModule.FileName.Equals(
                                    currentProcess.MainModule.FileName,
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                IntPtr existingWindowHandle = process.MainWindowHandle;

                                if (existingWindowHandle != IntPtr.Zero)
                                {
                                    if (NativeWindow.IsIconic(existingWindowHandle))
                                    {
                                        NativeWindow.ShowWindow(
                                            existingWindowHandle,
                                            NativeWindow.ShowWindowCommand.Restore
                                        );
                                    }

                                    NativeWindow.SetForegroundWindow(existingWindowHandle);
                                    focusedOtherProcess = true;
                                    break;
                                }

                                // If the process has no main window, try to bring it to the front using IPC
                                if (IPC.SendToFront())
                                {
                                    focusedOtherProcess = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error("Failed to focus existing instance: {0}", ex.Message);
                }

                if (!focusedOtherProcess)
                {
                    var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = I18n.GetString("i.running.title"),
                        Content = I18n.GetString("i.running.content"),
                        CloseButtonText = I18n.GetString("i.running.exit"),
                    };
                    await uiMessageBox.ShowDialogAsync();
                }

                Shutdown();
                return;
            }

            if (Environment.OSVersion.Version.Build < 17763)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("i.unsupported.os.title"),
                    Content = I18n.GetString("i.unsupported.os.content"),
                    CloseButtonText = I18n.GetString("i.unsupported.exit"),
                };
                await uiMessageBox.ShowDialogAsync();
                Shutdown();
                return;
            }

            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("i.unsupported.arch.title"),
                    Content = I18n.GetString("i.unsupported.arch.content"),
                    CloseButtonText = I18n.GetString("i.unsupported.exit"),
                };
                await uiMessageBox.ShowDialogAsync();
                Shutdown();
                return;
            }

            if (!EncryptionHelper.IsSupported())
            {
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("i.unsupported.vcpp.title"),
                    Content = I18n.GetString("i.unsupported.vcpp.content"),
                    CloseButtonText = I18n.GetString("i.unsupported.exit"),
                    PrimaryButtonText = I18n.GetString("i.unsupported.vcpp.download"),
                };
                var result = await uiMessageBox.ShowDialogAsync();
                if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
                {
                    Process.Start(
                        new ProcessStartInfo($"https://aka.ms/vs/17/release/vc_redist.x64.exe")
                        {
                            UseShellExecute = true,
                        }
                    );
                }
                Shutdown();
                return;
            }

            if (!await CheckForUnsupportedVersion(installationInfo.version))
            {
                Shutdown();
                return;
            }

            if (!autostart)
            {
                autostart = e.Args != null && e.Args.Contains("--autostart");
            }
            MainWindow mainWindow = new(autostart);
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }

        public void OnActivatedGuard(IActivatedEventArgs? e)
        {
            if (e != null && e.Kind == ActivationKind.StartupTask)
            {
                autostart = true;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            InactivityDetector.OnFocusGained();
            base.OnActivated(e);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            InactivityDetector.OnFocusLost();
            base.OnDeactivated(e);
        }

        /// <summary>
        /// This function checks if the current app version is supported based on the minimum app version stored in settings.
        /// The minimum app version is stored in the settings to prevent older versions from running after
        /// running a newer version that has breaking changes.
        /// Returns true if the current version is supported, false otherwise.
        /// </summary>
        /// <param name="currentVersion">The current app version</param>
        /// <returns></returns>
        private async Task<bool> CheckForUnsupportedVersion(Version currentVersion)
        {
            bool settingsChanged = false;

            // If minimum version is not initialized, set it to the current minimum version
            if (
                SettingsManager.Settings.MinimumAppVersion.Major == 0
                && SettingsManager.Settings.MinimumAppVersion.Minor == 0
            )
            {
                SettingsManager.Settings.MinimumAppVersion = minimumCompatibleVersion;
                settingsChanged = true;
            }

            // Increase minimum app version if changed
            if (minimumCompatibleVersion > SettingsManager.Settings.MinimumAppVersion)
            {
                SettingsManager.Settings.MinimumAppVersion = minimumCompatibleVersion;
                settingsChanged = true;
            }

            bool isCompatible = currentVersion >= SettingsManager.Settings.MinimumAppVersion;

            // Update LastUsedAppVersion to the current version if app can be started
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
                    "Preventing app start: Current version is older than minimum app version: {0} < {1}",
                    currentVersion,
                    SettingsManager.Settings.MinimumAppVersion
                );
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("i.unsupported.olderversion.title"),
                    Content = I18n.GetString("i.unsupported.olderversion.content"),
                    CloseButtonText = I18n.GetString("i.unsupported.exit"),
                };
                await uiMessageBox.ShowDialogAsync();
            }

            return isCompatible;
        }
    }
}
