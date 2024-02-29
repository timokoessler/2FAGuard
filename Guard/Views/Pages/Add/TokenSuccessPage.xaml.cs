using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Models;
using Guard.Views.UIComponents;

namespace Guard.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für TokenSuccessPage.xaml
    /// </summary>
    public partial class TokenSuccessPage : Page
    {
        private readonly MainWindow mainWindow;

        public TokenSuccessPage()
        {
            InitializeComponent();
            int tokenID = (int)NavigationContextManager.CurrentContext["tokenID"];
            string type = (string)NavigationContextManager.CurrentContext["type"];
            NavigationContextManager.ClearContext();

            mainWindow = (MainWindow)Application.Current.MainWindow;

            if (type.Equals("added"))
            {
                mainWindow.SetPageTitle(I18n.GetString("stp.added"));
            } else if (type.Equals("edited"))
            {
                mainWindow.SetPageTitle(I18n.GetString("stp.edited"));
            }

            TOTPTokenHelper? token = TokenManager.GetTokenById(tokenID);
            if (token == null)
            {
                mainWindow.Navigate(typeof(Home));
                return;
            }

            TokenCardContainer.Children.Insert(1, new TokenCard(token));
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Navigate(typeof(Home));
        }
    }
}
