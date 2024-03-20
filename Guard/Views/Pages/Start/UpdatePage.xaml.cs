using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Installation;

namespace Guard.Views.Pages.Start
{
    /// <summary>
    /// Interaktionslogik für Login.xaml
    /// </summary>
    public partial class UpdatePage : Page
    {
        private readonly MainWindow mainWindow;
        private readonly Updater.UpdateInfo updateInfo;

        public UpdatePage(Updater.UpdateInfo updateInfo)
        {
            InitializeComponent();
            this.updateInfo = updateInfo;
            mainWindow = (MainWindow)Application.Current.MainWindow;
        }

        private void NavigateHome()
        {
            mainWindow.FullContentFrame.Content = null;
            mainWindow.FullContentFrame.Visibility = Visibility.Collapsed;
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(Home));
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            NavigateHome();
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            QuestionPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;

            mainWindow.GetStatsClient()?.TrackEvent("UpdateStarted");
            bool isPortable = InstallationInfo.IsPortable();

            try
            {
                string downloadUrl = isPortable
                    ? updateInfo.Urls.Portable
                    : updateInfo.Urls.Installer;

                string downloadFileName = Path.GetFullPath(
                    isPortable
                        ? $"2FAGuard-Portable-{updateInfo.Version}.exe"
                        : Path.Combine(
                            Path.GetTempPath(),
                            $"2FAGuard-Installer-{updateInfo.Version}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.exe"
                        )
                );

                if (isPortable && File.Exists(downloadFileName))
                {
                    throw new Exception(
                        "You have already downloaded the newest portable version. Please start the new version instead of the old one."
                    );
                }

                using var stream = await Updater.httpClient.GetStreamAsync(downloadUrl);
                using FileStream fileStream =
                    new(downloadFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                await stream.CopyToAsync(fileStream);
                fileStream.Close();
                stream.Close();

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo(downloadFileName)
                    {
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    },
                };
                process.Start();
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(
                    "Error while downloading update: {0} {1}",
                    ex.Message,
                    ex.StackTrace
                );
                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("update.error.title"),
                    Content = $"{I18n.GetString("update.error.content")}: {ex.Message}",
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 500
                }.ShowDialogAsync();
                Application.Current.Shutdown(1);
            }
        }
    }
}
