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

        private async void Button_Skip_Click(object sender, RoutedEventArgs e)
        {
            var dialogResult = await mainWindow
                .GetContentDialogService()
                .ShowSimpleDialogAsync(
                    new SimpleContentDialogCreateOptions()
                    {
                        Title = I18n.GetString("welcome.insecure.dialog.title"),
                        Content = I18n.GetString("welcome.insecure.dialog.content"),
                        CloseButtonText = I18n.GetString("dialog.close"),
                        PrimaryButtonText = I18n.GetString("welcome.insecure.dialog.continue")
                    }
                );
            if (dialogResult == Wpf.Ui.Controls.ContentDialogResult.Primary)
            {
                try
                {
                    await Auth.Init();
                    await Auth.RegisterInsecure();
                    mainWindow.FullContentFrame.Content = new SetupCompleted();
                }
                catch (Exception ex)
                {
                    await mainWindow
                        .GetContentDialogService()
                        .ShowSimpleDialogAsync(
                            new SimpleContentDialogCreateOptions()
                            {
                                Title = "Error",
                                Content = ex.Message,
                                CloseButtonText = I18n.GetString("dialog.close"),
                            }
                        );
                }
            }
        }
    }
}
