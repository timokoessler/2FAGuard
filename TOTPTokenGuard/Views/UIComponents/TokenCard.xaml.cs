using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using OtpNet;
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

        internal TokenCard(TOTPTokenHelper token)
        {
            InitializeComponent();
            this.token = token;

            Update();
            ScheduleUpdates();
        }

        private void Update()
        {
            UpdateProgressRing();
            UpdateTokenText();
        }

        private async Task ScheduleUpdates()
        {
            while (true)
            {
                await Task.Delay(token.GetRemainingSeconds() * 1000);
                Update();
            }
        }

        private void UpdateProgressRing()
        {
            int totalSeconds = token.getInfo().Period ?? 30;
            int remainingSeconds = token.GetRemainingSeconds();

            TimeProgressRing.Progress = (double)remainingSeconds / totalSeconds * 100;

            Duration duration = new(TimeSpan.FromSeconds(remainingSeconds));
            DoubleAnimation doubleanimation = new(TimeProgressRing.Progress + (1 - TimeProgressRing.Progress), duration);
            TimeProgressRing.BeginAnimation(ProgressRing.ProgressProperty, doubleanimation);
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
