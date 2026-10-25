using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Guard.Core.Security;
using Guard.Linux.Core;
using Guard.WPF.Core;
using Guard.WPF.Core.Security;

namespace Guard.Linux.Views.Pages.Preferences
{
    public partial class ChangePasswordPage : UserControl
    {
        private readonly MainWindow mainWindow;
        private readonly bool insecure;

        public ChangePasswordPage()
        {
            InitializeComponent();
            mainWindow = App.MainWindow!;

            insecure = !Auth.IsLoginEnabled();
            CurrentPass.IsVisible = !insecure;
            Loaded += (_, _) => (insecure ? PasswordBox : CurrentPass).Focus();

            CapsLockDetector.Attach(CurrentPass, CapsLockWarning);
            CapsLockDetector.Attach(PasswordBox, CapsLockWarning);
            CapsLockDetector.Attach(PasswordBoxRepeat, CapsLockWarning);
        }

        private async void Button_Click(object? sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;
            string password = PasswordBox.Text ?? "";
            string current = CurrentPass.Text ?? "";

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Error", I18n.GetString("welcome.pass.notempty"));
                return;
            }
            if (!insecure && string.IsNullOrWhiteSpace(current))
            {
                ShowError("Error", I18n.GetString("changepass.current.notempty"));
                return;
            }

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
                if (!insecure && !Auth.CheckPassword(Encoding.UTF8.GetBytes(current)))
                {
                    throw new Exception(I18n.GetString("changepass.current.incorrect"));
                }
                await Auth.ChangePassword(Encoding.UTF8.GetBytes(password));
                mainWindow.Navigate(typeof(Settings));
            }
            catch (Exception ex)
            {
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
