using System.Windows;
using TOTPTokenGuard.Core;

namespace TOTPTokenGuard
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string MutexName = "TOTPTokenGuardSingleAppInstanceMutex";
        private Mutex? singleInstanceMutex;

        protected override async void OnStartup(StartupEventArgs e)
        {
            singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);

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
