using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Guard.Core;
using Guard.Core.Security;
using Guard.Linux.Core;
using Guard.WPF.Core;
using Guard.WPF.Core.Security;

namespace Guard.Linux.Views.Pages.Start
{
    public partial class SetupPassword : UserControl
    {
        private readonly bool enableKeyring;
        private readonly MainWindow mainWindow;

        public SetupPassword()
            : this(false) { }

        public SetupPassword(bool enableKeyring)
        {
            this.enableKeyring = enableKeyring;
            InitializeComponent();
            mainWindow = App.MainWindow!;
            StartPageHeader.Register(this, HeaderLogo, HeaderTitle, HeaderSubtitle);
            CapsLockDetector.Attach(PasswordBox, CapsLockWarning);
            CapsLockDetector.Attach(PasswordBoxRepeat, CapsLockWarning);
            Loaded += (_, _) => PasswordBox.Focus();
        }

        private async void Button_Click(object? sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;
            string password = PasswordBox.Text ?? "";

            (bool passValid, string? passInvalidReason) = PasswordComplexity.CheckPassword(password);
            if (!passValid)
            {
                ShowError("Error", passInvalidReason ?? "Invalid Password");
                return;
            }

            if (password != PasswordBoxRepeat.Text)
            {
                ShowError("Error", I18n.GetString("welcome.pass.notmatch"));
                return;
            }

            SaveButton.IsEnabled = false;
            RegisterProgressBar.IsVisible = true;

            try
            {
                await Auth.Init();
                await Auth.Register(Encoding.UTF8.GetBytes(password), enableKeyring);
                mainWindow.ShowFullContent(new SetupCompleted());
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Error during registration: {0} {1}", ex.Message, ex.StackTrace);
                RegisterProgressBar.IsVisible = false;
                ShowError("Error", ex.Message);
                SaveButton.IsEnabled = true;
            }
        }

        private void ShowError(string title, string message)
        {
            InfoBar.Title = title;
            InfoBar.Message = message;
            InfoBar.Severity = InfoBarSeverity.Error;
            InfoBar.IsOpen = true;
        }

        private void CapsLockWarning(bool capsLock)
        {
            string message = I18n.GetString("login.warning.capslock.content");
            if (capsLock)
            {
                InfoBar.Title = "";
                InfoBar.Message = message;
                InfoBar.Severity = InfoBarSeverity.Warning;
                InfoBar.IsOpen = true;
            }
            else if (InfoBar.Message == message)
            {
                InfoBar.IsOpen = false;
            }
        }
    }
}
