using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Installation;
using Guard.Core.Security;
using Wpf.Ui;
using Wpf.Ui.Extensions;

namespace Guard.Views.Pages.Start
{
    /// <summary>
    /// Interaktionslogik für Welcome.xaml
    /// </summary>
    public partial class Welcome : Page
    {
        private readonly MainWindow mainWindow;

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
                _ = await new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("welcome.hello.notavailable"),
                    Content = I18n.GetString("welcome.hello.notavailable.content"),
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400
                }.ShowDialogAsync();
                return;
            }
            if (InstallationInfo.IsPortable())
            {
                Wpf.Ui.Controls.MessageBoxResult sucessDialogResult =
                    await new Wpf.Ui.Controls.MessageBox
                    {
                        Title = I18n.GetString("welcome.portable.winhello.title"),
                        Content = I18n.GetString("welcome.portable.winhello.content"),
                        CloseButtonText = I18n.GetString("dialog.close"),
                        PrimaryButtonText = I18n.GetString("dialog.next"),
                        MaxWidth = 500
                    }.ShowDialogAsync();

                if (sucessDialogResult != Wpf.Ui.Controls.MessageBoxResult.Primary)
                {
                    return;
                }
            }
            mainWindow.FullContentFrame.Content = new SetupPassword(true);
        }

        private void CardAction_Password_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.FullContentFrame.Content = new SetupPassword(true);
        }

        private async void Button_Skip_Click(object sender, RoutedEventArgs e)
        {
            Wpf.Ui.Controls.MessageBoxResult sucessDialogResult =
                await new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("welcome.insecure.dialog.title"),
                    Content = I18n.GetString("welcome.insecure.dialog.content"),
                    CloseButtonText = I18n.GetString("dialog.close"),
                    PrimaryButtonText = I18n.GetString("welcome.insecure.dialog.continue"),
                    MaxWidth = 500
                }.ShowDialogAsync();

            if (sucessDialogResult != Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                return;
            }

            try
            {
                await Auth.Init();
                await Auth.RegisterInsecure();
                mainWindow.FullContentFrame.Content = new SetupCompleted();
            }
            catch (Exception ex)
            {
                _ = await new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Error",
                    Content = ex.Message,
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 500
                }.ShowDialogAsync();
            }
        }
    }
}
