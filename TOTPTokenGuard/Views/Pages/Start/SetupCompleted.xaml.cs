using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        }

        private void CardAction_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.FullContentFrame.Content = null;
            mainWindow.FullContentFrame.Visibility = Visibility.Collapsed;
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(Home));
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
