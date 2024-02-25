using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TOTPTokenGuard.Core.Icons;
using TOTPTokenGuard.Core.Models;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace TOTPTokenGuard.Views.UIComponents
{
    /// <summary>
    /// Interaktionslogik für TokenCard.xaml
    /// </summary>
    partial class TokenCard : UserControl
    {
        private readonly TOTPTokenHelper token;
        private DoubleAnimation? doubleAnimation;

        private readonly IconManager.TotpIcon? icon;

        internal TokenCard(TOTPTokenHelper token)
        {
            InitializeComponent();
            this.token = token;

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
            MouseLeftButtonUp += async (sender, e) =>
            {
                Clipboard.SetText(token.GenerateToken());
                TimeProgressRing.Visibility = Visibility.Collapsed;
                SvgIconRingView.Visibility = Visibility.Visible;
                await Task.Delay(1000);
                SvgIconRingView.Visibility = Visibility.Collapsed;
                TimeProgressRing.Visibility = Visibility.Visible;
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
    }
}
