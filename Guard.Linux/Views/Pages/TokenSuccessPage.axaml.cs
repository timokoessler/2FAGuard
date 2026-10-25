using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Guard.Core.Models;
using Guard.Linux.Views.Controls;
using Guard.WPF.Core;

namespace Guard.Linux.Views.Pages
{
    public partial class TokenSuccessPage : UserControl
    {
        private readonly MainWindow mainWindow;
        private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };

        public TokenSuccessPage()
        {
            InitializeComponent();
            mainWindow = App.MainWindow!;

            int tokenID = (int)NavigationContextManager.CurrentContext["tokenID"];
            string type = (string)NavigationContextManager.CurrentContext["type"];

            if (type != "added-multiple")
            {
                TOTPTokenHelper? token = TokenManager.GetTokenById(tokenID);
                if (token == null)
                {
                    NavigationContextManager.ClearContext();
                    Dispatcher.UIThread.Post(() => mainWindow.Navigate(typeof(Home)));
                    return;
                }

                TokenCard card = new(token);
                TokenCardContainer.Children.Insert(1, card);
                timer.Tick += (_, _) => card.Update();
            }
            else
            {
                int count = (int)NavigationContextManager.CurrentContext["count"];
                int duplicateCount = (int)NavigationContextManager.CurrentContext["duplicateCount"];
                int addedCount = count - duplicateCount;

                TokenCardContainer.Children.Insert(
                    1,
                    new TextBlock
                    {
                        Text = I18n.GetString("i.stp.added.multiple.description")
                            .Replace("@Count", addedCount.ToString()),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 16,
                    }
                );

                if (duplicateCount > 0)
                {
                    TokenCardContainer.Children.Insert(
                        2,
                        new TextBlock
                        {
                            Text = I18n.GetString("i.stp.added.multiple.duplicate")
                                .Replace("@Count", duplicateCount.ToString()),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            FontSize = 16,
                        }
                    );
                }
            }

            NavigationContextManager.ClearContext();

            Loaded += (_, _) =>
            {
                WPF.Core.EventManager.TokenDeleted += OnTokenDeleted;
                timer.Start();
                string? title = type switch
                {
                    "added" => I18n.GetString("stp.added"),
                    "edited" => I18n.GetString("stp.edited"),
                    "added-multiple" => I18n.GetString("stp.added.multiple"),
                    _ => null,
                };
                if (title != null)
                {
                    mainWindow.SetPageTitle(title);
                }
            };

            Unloaded += (_, _) =>
            {
                WPF.Core.EventManager.TokenDeleted -= OnTokenDeleted;
                timer.Stop();
            };
        }

        private void OnTokenDeleted(object? sender, int tokenId)
        {
            mainWindow.Navigate(typeof(Home));
        }

        private void HomeButton_Click(object? sender, RoutedEventArgs e)
        {
            mainWindow.Navigate(typeof(Home));
        }
    }
}
