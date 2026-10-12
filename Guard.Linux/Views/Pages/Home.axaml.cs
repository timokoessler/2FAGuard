using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Core.Storage;
using Guard.Linux.Core;
using Guard.Linux.Views.Controls;
using Guard.Linux.Views.Pages.Add;
using Guard.WPF.Core;

namespace Guard.Linux.Views.Pages
{
    public partial class Home : UserControl
    {
        private readonly MainWindow mainWindow;
        private SortOrderSetting currentSortOrder = SettingsManager.Settings.SortOrder;
        private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private bool isAttached;

        public Home()
        {
            InitializeComponent();
            mainWindow = App.MainWindow!;

            timer.Tick += (_, _) =>
            {
                foreach (TokenCard card in TOTPTokenContainer.Children)
                {
                    card.Update();
                }
            };

            Loaded += (_, _) =>
            {
                isAttached = true;
                WPF.Core.EventManager.TokenDeleted += OnTokenDeleted;
                WPF.Core.EventManager.WindowSizeChanged += OnWindowSizeChanged;
                mainWindow.PropertyChanged += OnMainWindowPropertyChanged;
                if (!Auth.IsLoggedIn())
                {
                    mainWindow.Logout();
                    return;
                }
                LoadTokens();
            };

            Unloaded += (_, _) =>
            {
                isAttached = false;
                WPF.Core.EventManager.TokenDeleted -= OnTokenDeleted;
                WPF.Core.EventManager.WindowSizeChanged -= OnWindowSizeChanged;
                mainWindow.PropertyChanged -= OnMainWindowPropertyChanged;
                timer.Stop();
                TOTPTokenContainer.Children.Clear();
                TOTPTokenContainer.IsVisible = false;
                SearchPanel.IsVisible = false;
                NoTokensInfo.IsVisible = false;
                LoadingInfo.IsVisible = true;
            };

            SearchBox.TextChanged += (_, _) =>
            {
                string search = SearchBox.Text ?? "";
                foreach (TokenCard card in TOTPTokenContainer.Children)
                {
                    card.IsVisible =
                        search.Length == 0
                        || card.SearchString.Contains(search, StringComparison.OrdinalIgnoreCase);
                }
            };

            bool small = mainWindow.Bounds.Height < 500;
            LoadingInfoImage.IsVisible = !small;
            OnWindowSizeChanged(null, (mainWindow.Bounds.Width, mainWindow.Bounds.Height));
        }

        private async void LoadTokens()
        {
            List<TOTPTokenHelper>? tokenHelpers = await TokenManager.GetAllTokens();

            if (!isAttached)
            {
                return;
            }

            if (tokenHelpers == null)
            {
                if (!Auth.IsLoggedIn())
                {
                    mainWindow.Logout();
                    return;
                }
                throw new Exception("Failed to load tokens (tokenHelpers is null)");
            }

            if (tokenHelpers.Count == 0)
            {
                LoadingInfo.IsVisible = false;
                NoTokensInfo.IsVisible = true;
                NoTokensInfoImage.IsVisible = mainWindow.Bounds.Height >= 500;
                return;
            }

            List<TokenCard> tokenCards = tokenHelpers.Select(t => new TokenCard(t)).ToList();
            SortTokenCardList(tokenCards);

            TOTPTokenContainer.Children.Clear();
            TOTPTokenContainer.Children.AddRange(tokenCards);

            LoadingInfo.IsVisible = false;
            NoTokensInfo.IsVisible = false;
            TOTPTokenContainer.IsVisible = true;
            SearchPanel.IsVisible = true;

            SearchBox.Focus();
            timer.Start();

            if (SettingsManager.Settings.ShowTokenCardIntro)
            {
                await Dialogs.MessageBox(
                    I18n.GetString("home.intro.title"),
                    I18n.GetString("home.intro.content")
                );
                SettingsManager.Settings.ShowTokenCardIntro = false;
                _ = SettingsManager.Save();
            }
        }

        // Don't waste cycles while minimized or hidden in the tray
        private void OnMainWindowPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != Window.WindowStateProperty && e.Property != IsVisibleProperty)
            {
                return;
            }
            bool visible = mainWindow.IsVisible && mainWindow.WindowState != WindowState.Minimized;
            if (!visible)
            {
                timer.Stop();
            }
            else if (TOTPTokenContainer.IsVisible && !timer.IsEnabled)
            {
                foreach (TokenCard card in TOTPTokenContainer.Children)
                {
                    card.Update();
                }
                timer.Start();
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
            if (TOTPTokenContainer.Children.Count == 0)
            {
                timer.Stop();
                TOTPTokenContainer.IsVisible = false;
                SearchPanel.IsVisible = false;
                NoTokensInfo.IsVisible = true;
            }
        }

        private void NoTokens_Button_Click(object? sender, RoutedEventArgs e)
        {
            mainWindow.Navigate(typeof(AddOverview));
        }

        private void Sort_Issuer_ASC(object? sender, RoutedEventArgs e) =>
            SortClicked(SortOrderSetting.ISSUER_ASC);

        private void Sort_Issuer_DESC(object? sender, RoutedEventArgs e) =>
            SortClicked(SortOrderSetting.ISSUER_DESC);

        private void Sort_CreatedAt_ASC(object? sender, RoutedEventArgs e) =>
            SortClicked(SortOrderSetting.CREATED_ASC);

        private void Sort_CreatedAt_DESC(object? sender, RoutedEventArgs e) =>
            SortClicked(SortOrderSetting.CREATED_DESC);

        private void SortClicked(SortOrderSetting sortOrder)
        {
            if (currentSortOrder == sortOrder)
            {
                return;
            }
            currentSortOrder = sortOrder;
            List<TokenCard> cards = TOTPTokenContainer.Children.Cast<TokenCard>().ToList();
            TOTPTokenContainer.Children.Clear();
            SortTokenCardList(cards);
            TOTPTokenContainer.Children.AddRange(cards);
            SettingsManager.Settings.SortOrder = sortOrder;
            _ = SettingsManager.Save();
        }

        private void SortTokenCardList(List<TokenCard> cards)
        {
            switch (currentSortOrder)
            {
                case SortOrderSetting.ISSUER_ASC:
                    cards.Sort((a, b) => string.Compare(a.GetIssuer(), b.GetIssuer(), StringComparison.CurrentCultureIgnoreCase));
                    break;
                case SortOrderSetting.ISSUER_DESC:
                    cards.Sort((a, b) => string.Compare(b.GetIssuer(), a.GetIssuer(), StringComparison.CurrentCultureIgnoreCase));
                    break;
                case SortOrderSetting.CREATED_ASC:
                    cards.Sort((a, b) => a.GetCreationTime().CompareTo(b.GetCreationTime()));
                    break;
                case SortOrderSetting.CREATED_DESC:
                    cards.Sort((a, b) => b.GetCreationTime().CompareTo(a.GetCreationTime()));
                    break;
            }
        }

        private void OnWindowSizeChanged(object? sender, (double width, double height) size)
        {
            SearchBox.Width = size.width < 600 ? Math.Max(120, size.width - 238) : 360;
        }
    }
}
