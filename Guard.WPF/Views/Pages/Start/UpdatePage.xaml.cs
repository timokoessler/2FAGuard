using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.WPF.Core;
using Guard.WPF.Core.Installation;

namespace Guard.WPF.Views.Pages.Start
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

            try
            {
                await Updater.Update(updateInfo);
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
