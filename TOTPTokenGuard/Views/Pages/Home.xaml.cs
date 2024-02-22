using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
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
        private MainWindow mainWindow;

        public Home()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            Loaded += async (sender, e) => await LoadDataInBackground();
        }
        private async Task LoadDataInBackground()
        {
            try
            {
                List<TOTPTokenHelper> tokenHelpers = [];

                await Task.Run(() =>
                {
                    string secret = Auth.GetMainEncryptionHelper().EncryptString("TEST");

                    for (int i = 0; i < 10; i++)
                    {
                        Random rand = new Random();
                        DBTOTPToken test = new DBTOTPToken
                        {
                            Id = 1,
                            Issuer = "ACME",
                            EncryptedSecret = secret,
                        };
                        tokenHelpers.Add(new TOTPTokenHelper(test));
                    }
                });

                foreach (var token in tokenHelpers)
                {
                    TokenCard card = new TokenCard(token);
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
