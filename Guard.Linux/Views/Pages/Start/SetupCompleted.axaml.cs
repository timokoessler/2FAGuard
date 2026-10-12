using Avalonia.Controls;
using Avalonia.Interactivity;
using Guard.Core.Storage;
using Guard.Linux.Core.Security;
using Guard.Linux.Views.Pages.Add;
using Guard.WPF.Core;

namespace Guard.Linux.Views.Pages.Start
{
    public partial class SetupCompleted : UserControl
    {
        private readonly MainWindow mainWindow;

        public SetupCompleted()
        {
            InitializeComponent();
            mainWindow = App.MainWindow!;
            StartPageHeader.Register(this, HeaderLogo, HeaderTitle, HeaderSubtitle);
            Database.Init();
            // Inits empty token list
            _ = TokenManager.GetAllTokens();
            InactivityDetector.Start();
        }

        private void CardAction_AddToken_Click(object? sender, RoutedEventArgs e)
        {
            mainWindow.ClearFullContentFrame();
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(AddOverview));
        }

        private void CardAction_Settings_Click(object? sender, RoutedEventArgs e)
        {
            mainWindow.ClearFullContentFrame();
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(Pages.Settings));
        }
    }
}
