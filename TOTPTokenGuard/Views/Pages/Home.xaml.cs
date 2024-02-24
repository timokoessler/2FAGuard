using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Core.Models;
using TOTPTokenGuard.Core.Security;
using TOTPTokenGuard.Views.UIComponents;

namespace TOTPTokenGuard.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        private readonly MainWindow mainWindow;

        public Home()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            Loaded += async (sender, e) => await LoadTokens();
        }

        private async Task LoadTokens()
        {
            try
            {
                List<TOTPTokenHelper>? tokenHelpers = await TokenManager.GetAllTokens();
                if (tokenHelpers == null)
                {
                    LoadingInfo.Visibility = Visibility.Visible;
                    TOTPTokenContainer.Visibility = Visibility.Collapsed;

                    // Show no tokens message
                    return;
                }

                foreach (var token in tokenHelpers)
                {
                    TokenCard card = new(token);
                    TOTPTokenContainer.Children.Add(card);
                }

                LoadingInfo.Visibility = Visibility.Collapsed;
                TOTPTokenContainer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }
    }
}
