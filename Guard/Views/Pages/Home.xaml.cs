using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Models;
using Guard.Views.Pages.Add;
using Guard.Views.UIComponents;

namespace Guard.Views.Pages
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

            Loaded += async (sender, e) => await LoadTokens();
            Core.EventManager.TokenUpdated += OnTokenUpdated;

            mainWindow = (MainWindow)Application.Current.MainWindow;
        }

        private async Task LoadTokens()
        {
            try
            {
                List<TOTPTokenHelper>? tokenHelpers =
                    await TokenManager.GetAllTokens()
                    ?? throw new Exception("Error loading tokens (tokenHelpers is null)");
                foreach (var token in tokenHelpers)
                {
                    TokenCard card = new(token);
                    TOTPTokenContainer.Children.Add(card);
                }

                if (tokenHelpers.Count == 0)
                {
                    LoadingInfo.Visibility = Visibility.Collapsed;
                    NoTokensInfo.Visibility = Visibility.Visible;
                    return;
                }

                LoadingInfo.Visibility = Visibility.Collapsed;
                TOTPTokenContainer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        private void OnTokenUpdated(object? sender, int tokenId)
        {
            LoadingInfo.Visibility = Visibility.Visible;
            TOTPTokenContainer.Visibility = Visibility.Collapsed;
            TOTPTokenContainer.Children.Clear();
            _ = LoadTokens();
        }

        private void NoTokens_Button_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Navigate(typeof(AddOverview));
        }
    }
}
