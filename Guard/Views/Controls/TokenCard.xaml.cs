using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Guard.Core;
using Guard.Core.Icons;
using Guard.Core.Models;
using Guard.Views.Pages;
using Guard.Views.Pages.Add;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Guard.Views.UIComponents
{
    /// <summary>
    /// Interaktionslogik für TokenCard.xaml
    /// </summary>
    partial class TokenCard : UserControl
    {
        private readonly TOTPTokenHelper token;
        private DoubleAnimation? doubleAnimation;
        private readonly MainWindow mainWindow;

        private readonly IconManager.TotpIcon? icon;

        internal TokenCard(TOTPTokenHelper token)
        {
            InitializeComponent();
            this.token = token;

            mainWindow = (MainWindow)Application.Current.MainWindow;

            Issuer.Text = token.dBToken.Issuer;
            if (token.dBToken.Username != null)
            {
                Username.Text = token.dBToken.Username;
            }
            else
            {
                Username.Visibility = Visibility.Collapsed;
            }

            if (token.dBToken.Icon != null && token.dBToken.IconType != null)
            {
                icon = IconManager.GetIcon(
                    token.dBToken.Icon,
                    IconManager.IconColor.Colored,
                    (IconManager.IconType)token.dBToken.IconType
                );

                SvgIconView.SvgSource = icon.Svg;
            }
            else
            {
                icon = IconManager.GetIcon(
                    "default",
                    IconManager.IconColor.Colored,
                    IconManager.IconType.Default
                );
                SvgIconView.SvgSource = icon.Svg;
            }

            UpdateTokenText();
            InitProgressRing();
            _ = ScheduleUpdates();

            // Add Click event to copy token to clipboard
            MouseLeftButtonUp += (sender, e) =>
            {
                CopyToken();
            };

            Cursor = Cursors.Hand;
        }

        private async Task ScheduleUpdates()
        {
            while (true)
            {
                int remainingSeconds = token.GetRemainingSeconds();
                if (remainingSeconds > 6)
                {
                    await Task.Delay((token.GetRemainingSeconds() - 5) * 1000);
                }
                TimeProgressRing.Foreground = Brushes.Red;

                await Task.Delay(token.GetRemainingSeconds() * 1000);
                UpdateTokenText();
                TimeProgressRing.Foreground = ApplicationAccentColorManager.PrimaryAccentBrush;
                if (doubleAnimation != null)
                {
                    doubleAnimation.From = 100;
                    doubleAnimation.Duration = TimeSpan.FromSeconds(token.dBToken.Period ?? 30);
                    TimeProgressRing.BeginAnimation(ProgressRing.ProgressProperty, doubleAnimation);
                }
            }
        }

        private void InitProgressRing()
        {
            int totalSeconds = token.dBToken.Period ?? 30;
            int remainingSeconds = token.GetRemainingSeconds();

            int start = (int)((double)remainingSeconds / (double)totalSeconds * 100);

            doubleAnimation = new()
            {
                From = start,
                To = 0,
                Duration = TimeSpan.FromSeconds(remainingSeconds),
            };
            TimeProgressRing.BeginAnimation(ProgressRing.ProgressProperty, doubleAnimation);
        }

        private void UpdateTokenText()
        {
            string tokenStr = token.GenerateToken();
            if (tokenStr.Length == 6)
            {
                tokenStr = tokenStr[..3] + " " + tokenStr[3..];
            }
            else if (tokenStr.Length == 8)
            {
                tokenStr = tokenStr[..4] + " " + tokenStr[4..];
            }
            TokenTextBlock.Text = tokenStr;
        }

        private async void CopyToken()
        {
            try
            {
                Clipboard.SetText(token.GenerateToken());
                TimeProgressRing.Visibility = Visibility.Collapsed;
                SvgIconRingView.Visibility = Visibility.Visible;
                await Task.Delay(1000);
                SvgIconRingView.Visibility = Visibility.Collapsed;
                TimeProgressRing.Visibility = Visibility.Visible;
            }
            catch
            {
                // Can happen if user makes really fast clicks
            }
        }

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            CopyToken();
        }

        private void MenuItem_Edit_Click(object sender, RoutedEventArgs e)
        {
            NavigationContextManager.CurrentContext["tokenID"] = token.dBToken.Id;
            NavigationContextManager.CurrentContext["action"] = "edit";
            mainWindow.Navigate(typeof(TokenSettings));
        }

        private async void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            var deleteMessageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = I18n.GetString("tokencard.delete.modal.title"),
                Content = I18n.GetString("tokencard.delete.modal.content")
                    .Replace("@Name", token.dBToken.Issuer),
                IsPrimaryButtonEnabled = true,
                PrimaryButtonText = I18n.GetString("tokencard.delete.modal.yes"),
                CloseButtonText = I18n.GetString("dialog.close"),
                MaxWidth = 400
            };

            var result = await deleteMessageBox.ShowDialogAsync();
            if (result == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                TokenManager.DeleteTokenById(token.dBToken.Id);
                if (mainWindow.GetActivePage()?.Name != "Home")
                {
                    mainWindow.Navigate(typeof(Home));
                    return;
                }
                Core.EventManager.EmitTokenUpdated(token.dBToken.Id);
            }
        }

        internal string GetSearchString()
        {
            return $"{token.dBToken.Issuer.ToLower()} {token.dBToken.Username?.ToLower()}";
        }
    }
}
