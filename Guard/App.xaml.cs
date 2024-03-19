using Guard.Core;
using Guard.Core.Installation;
using Guard.Core.Storage;
using System.Windows;

namespace Guard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex? singleInstanceMutex;

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

            bool autostart = e.Args != null && e.Args.Contains("--autostart");
            MainWindow mainWindow = new(autostart);
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }

    }
}
