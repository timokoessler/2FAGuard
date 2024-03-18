using System.Windows;
using Guard.Core;
using Guard.Core.Installation;

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

            if (!createdNew)
            {
                // Another instance is already running
                var uiMessageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "App is already running / App läuft bereits",
                    Content =
                        "You can only run one instance of this app at a time.\n"
                        + "Sie können diese App nur einmal gleichzeitig ausführen.",
                    CloseButtonText = "Exit / Beenden"
                };

                await uiMessageBox.ShowDialogAsync();
                Shutdown();
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
