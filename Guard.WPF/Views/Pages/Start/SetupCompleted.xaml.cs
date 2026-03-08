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

            Loaded += (object? sender, RoutedEventArgs e) =>
            {
                Core.EventManager.WindowSizeChanged += OnWindowSizeChanged;
                OnWindowSizeChanged(null, (mainWindow.ActualWidth, mainWindow.ActualHeight));
            };

            Unloaded += (object? sender, RoutedEventArgs e) =>
            {
                Core.EventManager.WindowSizeChanged -= OnWindowSizeChanged;
            };
        }

        private void CardAction_AddToken_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ClearFullContentFrame();
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(AddOverview));
        }

        private void CardAction_Settings_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ClearFullContentFrame();
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
