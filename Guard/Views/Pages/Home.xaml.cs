using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Models;
using Guard.Core.Storage;
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
        private SortOrderSetting currentSortOrder = SettingsManager.Settings.SortOrder;

        public Home()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            Loaded += async (sender, e) => await LoadTokens();
            Core.EventManager.TokenUpdated += OnTokenUpdated;

            SearchBox.TextChanged += (sender, e) =>
            {
                foreach (TokenCard card in TOTPTokenContainer.Children)
                {
                    if (SearchBox.Text.Length == 0)
                    {
                        card.Visibility = Visibility.Visible;
                        continue;
                    }
                    if (
                        card.SearchString.Contains(
                            SearchBox.Text,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        card.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        card.Visibility = Visibility.Collapsed;
                    }
                }
            };
        }

        private async Task LoadTokens()
        {
            List<TOTPTokenHelper>? tokenHelpers =
                await TokenManager.GetAllTokens()
                ?? throw new Exception("Error loading tokens (tokenHelpers is null)");

            if (tokenHelpers.Count == 0)
            {
                LoadingInfo.Visibility = Visibility.Collapsed;
                NoTokensInfo.Visibility = Visibility.Visible;
                return;
            }

            List<TokenCard> tokenCards = [];

            foreach (var token in tokenHelpers)
            {
                tokenCards.Add(new TokenCard(token));
            }

            SortTokenCardList(tokenCards);

            foreach (var card in tokenCards)
            {
                TOTPTokenContainer.Children.Add(card);
            }

            LoadingInfo.Visibility = Visibility.Collapsed;
            TOTPTokenContainer.Visibility = Visibility.Visible;
            SearchPanel.Visibility = Visibility.Visible;
        }

        private void OnTokenUpdated(object? sender, int tokenId)
        {
            LoadingInfo.Visibility = Visibility.Visible;
            TOTPTokenContainer.Visibility = Visibility.Collapsed;
            SearchPanel.Visibility = Visibility.Collapsed;
            TOTPTokenContainer.Children.Clear();
            _ = LoadTokens();
        }

        private void NoTokens_Button_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Navigate(typeof(AddOverview));
        }

        private void Sort_Issuer_ASC(object sender, RoutedEventArgs e)
        {
            SortClicked(SortOrderSetting.ISSUER_ASC);
        }

        private void Sort_Issuer_DESC(object sender, RoutedEventArgs e)
        {
            SortClicked(SortOrderSetting.ISSUER_DESC);
        }

        private void Sort_CreatedAt_ASC(object sender, RoutedEventArgs e)
        {
            SortClicked(SortOrderSetting.CREATED_ASC);
        }

        private void Sort_CreatedAt_DESC(object sender, RoutedEventArgs e)
        {
            SortClicked(SortOrderSetting.CREATED_DESC);
        }

        private void SortClicked(SortOrderSetting sortOrder)
        {
            if (currentSortOrder == sortOrder)
            {
                return;
            }
            currentSortOrder = sortOrder;
            LoadingInfo.Visibility = Visibility.Visible;
            TOTPTokenContainer.Visibility = Visibility.Collapsed;
            SearchPanel.Visibility = Visibility.Collapsed;
            List<TokenCard> cards = TOTPTokenContainer.Children.Cast<TokenCard>().ToList();
            TOTPTokenContainer.Children.Clear();
            SortTokenCardList(cards);
            foreach (var card in cards)
            {
                TOTPTokenContainer.Children.Add(card);
            }
            LoadingInfo.Visibility = Visibility.Collapsed;
            TOTPTokenContainer.Visibility = Visibility.Visible;
            SearchPanel.Visibility = Visibility.Visible;
            SettingsManager.Settings.SortOrder = sortOrder;
            _ = SettingsManager.Save();
        }

        private void SortTokenCardList(List<TokenCard> cards)
        {
            switch (currentSortOrder)
            {
                case SortOrderSetting.ISSUER_ASC:
                    cards.Sort((a, b) => a.GetIssuer().CompareTo(b.GetIssuer()));
                    break;
                case SortOrderSetting.ISSUER_DESC:
                    cards.Sort((a, b) => b.GetIssuer().CompareTo(a.GetIssuer()));
                    break;
                case SortOrderSetting.CREATED_ASC:
                    cards.Sort((a, b) => a.GetCreationTime().CompareTo(b.GetCreationTime()));
                    break;
                case SortOrderSetting.CREATED_DESC:
                    cards.Sort((a, b) => b.GetCreationTime().CompareTo(a.GetCreationTime()));
                    break;
            }
        }
    }
}
