using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using TOTPTokenGuard.Core.Models;
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
            ScheduleUpdates();
        }


        private async Task ScheduleUpdates()
        {
            while (true)
            {
                await Task.Delay(token.GetRemainingSeconds() * 1000);
                UpdateTokenText();
                TimeProgressRing.BeginAnimation(ProgressRing.ProgressProperty, null);
                TimeProgressRing.Progress = 100;
                doubleAnimation.Duration = TimeSpan.FromSeconds(token.getInfo().Period ?? 30);
                TimeProgressRing.BeginAnimation(ProgressRing.ProgressProperty, doubleAnimation);
            }
        }

        private void InitProgressRing()
        {
            int totalSeconds = token.getInfo().Period ?? 30;
            int remainingSeconds = token.GetRemainingSeconds();

            TimeProgressRing.Progress = (double)remainingSeconds / totalSeconds * 100;

            doubleAnimation = new(TimeProgressRing.Progress - (100/totalSeconds), new Duration(TimeSpan.FromSeconds(remainingSeconds)))
            {
                RepeatBehavior = RepeatBehavior.Forever
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
