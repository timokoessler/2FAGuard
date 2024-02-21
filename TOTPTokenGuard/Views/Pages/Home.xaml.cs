using System.Windows;
using System.Windows.Controls;
using TOTPTokenGuard.Views.UIComponents;

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

            TOTPTokenContainer.Children.Add(
                new TokenCard(
                    new Core.Models.TOTPToken
                    {
                        Id = 1,
                        Issuer = "Test",
                        EncryptedSecret = "JBSWY3"
                    }
                )
            );
        }
    }
}
