using Avalonia.Controls;
using Avalonia.Interactivity;
using Guard.Core;
using Guard.Core.Security;
using Guard.Linux.Core;
using Guard.WPF.Core;

namespace Guard.Linux.Views.Pages.Start
{
    public partial class Welcome : UserControl
    {
        private readonly MainWindow mainWindow;

        public Welcome()
        {
            InitializeComponent();
            mainWindow = App.MainWindow!;
            mainWindow.HideNavigation();
            StartPageHeader.Register(this, HeaderLogo, HeaderTitle, HeaderSubtitle);

            Loaded += (_, _) => ApplyRegistrySettings();
        }

        private void ApplyRegistrySettings()
        {
            if (mainWindow.SkipApplyRegistrySettings)
            {
                mainWindow.SkipApplyRegistrySettings = false;
                return;
            }
            (bool hideSkip, bool hideWinHello, bool hidePassword) =
                RegistrySettings.GetSetupHideOptions();

            SkipBtn.IsVisible = !hideSkip;
            WinHelloBtn.IsVisible = !hideWinHello;
            PasswordBtn.IsVisible = !hidePassword;

            if (hideSkip && hidePassword)
            {
                SetupWithKeyring();
            }
            if (hideSkip && hideWinHello)
            {
                SetupWithPassword();
            }
            if (hideWinHello && hidePassword)
            {
                SetupInsecure(false);
            }
        }

        private void CardAction_WinHello_Click(object? sender, RoutedEventArgs e)
        {
            SetupWithKeyring();
        }

        private async void SetupWithKeyring()
        {
            if (!await Task.Run(Auth.IsKeyringUnlockAvailable))
            {
                await Dialogs.MessageBox(
                    I18n.GetString("welcome.hello.notavailable"),
                    I18n.GetString("welcome.hello.notavailable.content")
                );
                return;
            }
            if (InstallationContext.IsPortable())
            {
                var result = await Dialogs.MessageBox(
                    I18n.GetString("welcome.portable.winhello.title"),
                    I18n.GetString("welcome.portable.winhello.content"),
                    primaryButtonText: I18n.GetString("dialog.next"),
                    maxWidth: 500
                );
                if (result != DialogResult.Primary)
                {
                    return;
                }
            }
            mainWindow.ShowFullContent(new SetupPassword(true));
        }

        private void CardAction_Password_Click(object? sender, RoutedEventArgs e)
        {
            SetupWithPassword();
        }

        private void SetupWithPassword()
        {
            mainWindow.ShowFullContent(new SetupPassword(false));
        }

        private void Button_Skip_Click(object? sender, RoutedEventArgs e)
        {
            SetupInsecure();
        }

        private async void SetupInsecure(bool showWarning = true)
        {
            if (showWarning)
            {
                var result = await Dialogs.MessageBox(
                    I18n.GetString("welcome.insecure.dialog.title"),
                    I18n.GetString("welcome.insecure.dialog.content"),
                    primaryButtonText: I18n.GetString("welcome.insecure.dialog.continue"),
                    maxWidth: 500
                );
                if (result != DialogResult.Primary)
                {
                    return;
                }
            }

            try
            {
                await Auth.Init();
                await Auth.RegisterInsecure();
                mainWindow.ShowFullContent(new SetupCompleted());
            }
            catch (Exception ex)
            {
                await Dialogs.MessageBox("Error", ex.Message, maxWidth: 500);
            }
        }
    }
}
