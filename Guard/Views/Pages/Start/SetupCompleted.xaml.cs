using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Storage;
using Guard.Views.Pages.Add;

namespace Guard.Views.Pages.Start
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
            mainWindow.GetStatsClient()?.TrackEvent("SetupCompleted");
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
