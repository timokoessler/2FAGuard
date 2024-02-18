using System.Windows;
using System.Windows.Controls;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Core.Security;
using Wpf.Ui;
using Wpf.Ui.Extensions;

namespace TOTPTokenGuard.Views.Pages.Start
{
    /// <summary>
    /// Interaktionslogik für Welcome.xaml
    /// </summary>
    public partial class Welcome : Page
    {
        private MainWindow mainWindow;

        public Welcome()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.HideNavigation();
        }

        private async void CardAction_WinHello_Click(object sender, RoutedEventArgs e)
        {
            if (!await WindowsHello.IsAvailable())
            {
                await mainWindow
                    .GetContentDialogService()
                    .ShowSimpleDialogAsync(
                        new SimpleContentDialogCreateOptions()
                        {
                            Title = I18n.GetString("welcome.hello.notavailable"),
                            Content = I18n.GetString("welcome.hello.notavailable.content"),
                            CloseButtonText = I18n.GetString("dialog.close")
                        }
                    );
                return;
            }
            mainWindow.FullContentFrame.Content = new SetupPassword(true);
        }

        private void CardAction_Password_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.FullContentFrame.Content = new SetupPassword(true);
        }

        private void Button_Skip_Click(object sender, RoutedEventArgs e)
        {
            //Todo Implement
        }
    }
}
