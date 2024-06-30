using System.Windows;
using System.Windows.Controls;
using Guard.Core.Security;
using Guard.Core.Storage;
using Guard.WPF.Core;
using Guard.WPF.Core.Security;
using Guard.WPF.Views.Pages.Add;

namespace Guard.WPF.Views.Pages.Start
{
    /// <summary>
    /// Interaktionslogik für SetupCompleted.xaml
    /// </summary>
    public partial class SetupCompleted : Page
    {
        private readonly MainWindow mainWindow;

        public SetupCompleted()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            Database.Init();
            // Inits empty token list
            _ = TokenManager.GetAllTokens();
            if (Auth.IsLoginEnabled())
            {
                mainWindow.GetStatsClient()?.TrackEvent("SetupCompleted");
            }
            else
            {
                mainWindow.GetStatsClient()?.TrackEvent("SetupCompletedInsecure");
            }
            InactivityDetector.Start();
        }

        private void CardAction_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.FullContentFrame.Content = null;
            mainWindow.FullContentFrame.Visibility = Visibility.Collapsed;
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(AddOverview));
        }

        private void CardAction_Click_Settings(object sender, RoutedEventArgs e)
        {
            mainWindow.FullContentFrame.Content = null;
            mainWindow.FullContentFrame.Visibility = Visibility.Collapsed;
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(Settings));
        }
    }
}
