using System.Windows;
using System.Windows.Controls;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Views.Pages.Add;

namespace TOTPTokenGuard.Views.Pages.Start
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
