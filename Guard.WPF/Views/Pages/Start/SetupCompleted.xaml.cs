using System.Windows;
using System.Windows.Controls;
using Guard.Core.Security;
using Guard.Core.Storage;
using Guard.WPF.Core;
using Guard.WPF.Core.Security;
using Guard.WPF.Views.Pages.Add;

namespace Guard.WPF.Views.Pages.Start
{
    /// <summary>
    /// Interaktionslogik für SetupCompleted.xaml
    /// </summary>
    public partial class SetupCompleted : Page
    {
        private readonly MainWindow mainWindow;

        public SetupCompleted()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            Database.Init();
            // Inits empty token list
            _ = TokenManager.GetAllTokens();
            Stats.TrackEvent(Stats.EventType.SetupCompleted);
            InactivityDetector.Start();

            Core.EventManager.WindowSizeChanged += OnWindowSizeChanged;

            Unloaded += (object? sender, RoutedEventArgs e) =>
            {
                Core.EventManager.WindowSizeChanged -= OnWindowSizeChanged;
            };

            OnWindowSizeChanged(null, (mainWindow.ActualWidth, mainWindow.ActualHeight));
        }

        private void CardAction_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.FullContentFrame.Content = null;
            mainWindow.FullContentFrame.Visibility = Visibility.Collapsed;
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(AddOverview));
        }

        private void CardAction_Click_Settings(object sender, RoutedEventArgs e)
        {
            mainWindow.FullContentFrame.Content = null;
            mainWindow.FullContentFrame.Visibility = Visibility.Collapsed;
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(Settings));
        }

        private void OnWindowSizeChanged(object? sender, (double width, double height) size)
        {
            if (mainWindow.ActualHeight < 600)
            {
                HeaderLogo.Visibility = Visibility.Collapsed;
                HeaderTitle.Visibility = Visibility.Collapsed;
                var margin = HeaderSubtitle.Margin;
                margin.Top = 60;
                HeaderSubtitle.Margin = margin;
            }
            else
            {
                HeaderLogo.Visibility = Visibility.Visible;
                HeaderTitle.Visibility = Visibility.Visible;
                var margin = HeaderSubtitle.Margin;
                margin.Top = 20;
                HeaderSubtitle.Margin = margin;
            }
        }
    }
}
