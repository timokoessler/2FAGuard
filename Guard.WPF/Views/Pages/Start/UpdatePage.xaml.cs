using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.WPF.Core;
using Guard.WPF.Core.Installation;
using Guard.WPF.Core.Models;

namespace Guard.WPF.Views.Pages.Start
{
    /// <summary>
    /// Interaktionslogik für Login.xaml
    /// </summary>
    public partial class UpdatePage : Page
    {
        private readonly MainWindow mainWindow;
        private readonly UpdateInfo? updateInfo;

        public UpdatePage()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            updateInfo = Updater.GetUpdateInfo();

            Core.EventManager.WindowSizeChanged += OnWindowSizeChanged;

            Unloaded += (object? sender, RoutedEventArgs e) =>
            {
                Core.EventManager.WindowSizeChanged -= OnWindowSizeChanged;
            };

            OnWindowSizeChanged(null, (mainWindow.ActualWidth, mainWindow.ActualHeight));
        }

        private void NavigateHome()
        {
            mainWindow.Navigate(typeof(Home), false);
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            NavigateHome();
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            QuestionPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;

            Stats.TrackEvent(Stats.EventType.UpdateStarted);

            try
            {
                if (updateInfo == null)
                {
                    throw new Exception("Update information is null.");
                }

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
                    MaxWidth = 500,
                }.ShowDialogAsync();
                Application.Current.Shutdown(1);
            }
        }

        private void OnWindowSizeChanged(object? sender, (double width, double height) size)
        {
            if (mainWindow.ActualHeight < 500)
            {
                HeaderLogo.Visibility = Visibility.Collapsed;
                HeaderTitle.Visibility = Visibility.Collapsed;
                var margin = HeaderSubtitle.Margin;
                margin.Top = 60;
                HeaderSubtitle.Margin = margin;
            }
            else
            {
                HeaderLogo.Visibility = Visibility.Visible;
                HeaderTitle.Visibility = Visibility.Visible;
                var margin = HeaderSubtitle.Margin;
                margin.Top = 20;
                HeaderSubtitle.Margin = margin;
            }
        }
    }
}
