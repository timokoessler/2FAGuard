using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Guard.Core;
using Guard.Core.Security;
using Guard.Core.Storage;
using Guard.Linux.Core;
using Guard.Linux.Core.Security;
using Guard.WPF.Core;

namespace Guard.Linux.Views.Pages.Start
{
    public partial class Login : UserControl
    {
        private readonly MainWindow mainWindow;

        public Login()
            : this(true) { }

        public Login(bool promptKeyring)
        {
            InitializeComponent();
            mainWindow = App.MainWindow!;
            StartPageHeader.Register(this, HeaderLogo, HeaderTitle, HeaderSubtitle, 500);
            CapsLockDetector.Attach(PasswordBox, CapsLockWarning);
            Loaded += (_, _) => PasswordBox.Focus();
            Setup(promptKeyring);
        }

        private async void Setup(bool promptKeyring)
        {
            try
            {
                await Auth.Init();
                if (!Auth.IsLoginEnabled())
                {
                    PasswordBox.IsEnabled = false;
                    DisableButtons();
                    Auth.LoginInsecure();
                    OnLoggedIn();
                    return;
                }
                WebAuthnBtn.IsVisible = Auth.GetWebAuthnDevices().Count > 0;
                if (Auth.IsKeyringUnlockRegistered())
                {
                    if (promptKeyring)
                    {
                        await LoginWithKeyring();
                    }
                }
                else
                {
                    KeyringButton.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                EnableButtons();
                LoginProgressBar.IsVisible = false;
                Log.Logger.Error("Error during login setup: {0}", ex.Message);
                ShowError("Error", ex.Message);
            }
        }

        private void ShowError(string title, string message)
        {
            if (message.Contains("Failed to decrypt keys"))
            {
                message = I18n.GetString("login.pass.error");
            }
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

        private void LoginButton_Click(object? sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;
            if (string.IsNullOrWhiteSpace(PasswordBox.Text))
            {
                ShowError("Error", I18n.GetString("welcome.pass.notempty"));
                return;
            }

            DisableButtons();
            LoginProgressBar.IsVisible = true;

            try
            {
                Auth.LoginWithPassword(Encoding.UTF8.GetBytes(PasswordBox.Text));
                OnLoggedIn();
            }
            catch (Exception ex)
            {
                LoginProgressBar.IsVisible = false;
                ShowError("Error", ex.Message);
                EnableButtons();
            }
        }

        private void OnLoggedIn()
        {
            if (!Auth.IsLoggedIn())
            {
                return;
            }
            PasswordBox.Text = "";
            Database.Init();

            mainWindow.ClearFullContentFrame();
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(Pages.Home));

            InactivityDetector.Start();
        }

        private async void KeyringButton_Click(object? sender, RoutedEventArgs e)
        {
            await LoginWithKeyring();
        }

        private async Task LoginWithKeyring()
        {
            InfoBar.IsOpen = false;
            DisableButtons();
            LoginProgressBar.IsVisible = true;

            try
            {
                await Auth.LoginWithKeyring();
                OnLoggedIn();
            }
            catch (Exception ex)
            {
                LoginProgressBar.IsVisible = false;
                EnableButtons();

                if (ex is OperationCanceledException || ex.Message.Contains("UserCanceled"))
                {
                    return;
                }
                Log.Logger.Error("Keyring login failed: {0}", ex.Message);
                ShowError("Error", ex.Message);
            }
        }

        private void EnableButtons()
        {
            LoginButton.IsEnabled = true;
            KeyringButton.IsEnabled = true;
            WebAuthnBtn.IsEnabled = true;
        }

        private void DisableButtons()
        {
            LoginButton.IsEnabled = false;
            KeyringButton.IsEnabled = false;
            WebAuthnBtn.IsEnabled = false;
        }

        private async void WebAuthnBtn_Click(object? sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;
            DisableButtons();

            try
            {
                var (device, pin) = await SecurityKeyFlow.PrepareDevice();
                LoginProgressBar.IsVisible = true;
                InfoBar.Title = "";
                InfoBar.Message = I18n.GetString("linux.fido2.touch");
                InfoBar.Severity = InfoBarSeverity.Informational;
                InfoBar.IsOpen = true;

                await Auth.LoginWithSecurityKey(device, pin);
                InfoBar.IsOpen = false;
                OnLoggedIn();
            }
            catch (Exception ex)
            {
                LoginProgressBar.IsVisible = false;
                InfoBar.IsOpen = false;
                EnableButtons();

                if (SecurityKeyFlow.IsCanceled(ex))
                {
                    return;
                }
                Log.Logger.Error("Security key login failed: {0}", ex.Message);
                ShowError("Error", ex.Message);
            }
        }
    }
}
