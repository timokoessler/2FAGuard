using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Models;
using Guard.Views.UIComponents;

namespace Guard.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        public Home()
        {
            InitializeComponent();

            Loaded += async (sender, e) => await LoadTokens();
            Core.EventManager.TokenUpdated += OnTokenUpdated;
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
                    LoadingText.Visibility = Visibility.Collapsed;
                    LoadingProgressBar.Visibility = Visibility.Collapsed;
                    NoTokensText.Visibility = Visibility.Visible;
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
    }
}
