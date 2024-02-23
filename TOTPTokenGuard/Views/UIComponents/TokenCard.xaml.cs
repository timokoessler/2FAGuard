using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

        internal TokenCard(TOTPTokenHelper token)
        {
            InitializeComponent();
            this.token = token;

            UpdateTokenText();
            InitProgressRing();
            _ = ScheduleUpdates();
        }

        private async Task ScheduleUpdates()
        {
            while (true)
            {
                int remainingSeconds = token.GetRemainingSeconds();
                if(remainingSeconds > 6)
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
                    doubleAnimation.Duration = TimeSpan.FromSeconds(token.getInfo().Period ?? 30);
                    TimeProgressRing.BeginAnimation(ProgressRing.ProgressProperty, doubleAnimation);
                }
            }
        }

        private void InitProgressRing()
        {
            int totalSeconds = token.getInfo().Period ?? 30;
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
