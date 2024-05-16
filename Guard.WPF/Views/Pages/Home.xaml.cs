using System.Windows;
using System.Windows.Controls;
using Guard.WPF.Core;
using Guard.WPF.Core.Models;
using Guard.WPF.Core.Storage;
using Guard.WPF.Views.Pages.Add;
using Guard.WPF.Views.UIComponents;

namespace Guard.WPF.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        private readonly MainWindow mainWindow;
        private SortOrderSetting currentSortOrder = SettingsManager.Settings.SortOrder;
        private PeriodicTimer? timer;

        public Home()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            Loaded += (sender, e) => LoadTokens();
            Core.EventManager.TokenDeleted += OnTokenDeleted;

            Unloaded += (object? sender, RoutedEventArgs e) =>
            {
                Core.EventManager.TokenDeleted -= OnTokenDeleted;
                timer?.Dispose();
            };

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

        private async void LoadTokens()
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

            RunTimer();

            if (SettingsManager.Settings.ShowTokenCardIntro)
            {
                _ = await new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("home.intro.title"),
                    Content = I18n.GetString("home.intro.content"),
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400
                }.ShowDialogAsync();
                SettingsManager.Settings.ShowTokenCardIntro = false;
                _ = SettingsManager.Save();
            }
        }

        private async void RunTimer()
        {
            timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            while (await timer.WaitForNextTickAsync())
            {
                foreach (TokenCard card in TOTPTokenContainer.Children)
                {
                    card.Update();
                }
            }
        }

        private void OnTokenDeleted(object? sender, int tokenId)
        {
            foreach (TokenCard card in TOTPTokenContainer.Children)
            {
                if (card.GetTokenId() == tokenId)
                {
                    TOTPTokenContainer.Children.Remove(card);
                    break;
                }
            }
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
