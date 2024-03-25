using Guard.Core;
using Guard.Core.Installation;
using Guard.Core.Security;
using Guard.Core.Storage;
using NSec.Cryptography;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Windows.ApplicationModel.Activation;

namespace Guard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex? singleInstanceMutex;
        private bool autostart = false;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            string mutexName = "2FAGuard";

            InstallationType installationType = InstallationInfo.GetInstallationType();
            if (installationType == InstallationType.CLASSIC_PORTABLE)
            {
                mutexName += "Portable";
            }
            else if (installationType == InstallationType.MICROSOFT_STORE)
            {
                mutexName += "Store";
            }

            singleInstanceMutex = new Mutex(true, mutexName, out bool createdNew);

            Log.Init();
            SettingsManager.Init();
            I18n.Init();

            if (!createdNew)
            {
                // Another instance is already running
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("i.running.title"),
                    Content = I18n.GetString("i.running.content"),
                    CloseButtonText = I18n.GetString("i.running.exit"),
                };
                await uiMessageBox.ShowDialogAsync();
                Shutdown();
                return;
            }

            if (Environment.OSVersion.Version.Build < 18362)
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
            try
            {
                _ = new Aegis256();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(
                    "Failed to initialize NSec.Cryptography.Aegis256 {0} {1}",
                    ex.Message,
                    ex.StackTrace
                );
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
    }
}
