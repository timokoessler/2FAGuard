using System.Windows;
using System.Windows.Controls;

namespace TOTPTokenGuard.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        private MainWindow mainWindow;

        public Home()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
        }
    }
}
