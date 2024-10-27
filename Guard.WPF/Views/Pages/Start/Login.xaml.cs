using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Guard.Core;
using Guard.Core.Security;
using Guard.Core.Storage;
using Guard.WPF.Core;
using Guard.WPF.Core.Installation;
using Guard.WPF.Core.Security;

namespace Guard.WPF.Views.Pages.Start
{
    /// <summary>
    /// Interaktionslogik für Login.xaml
    /// </summary>
    public partial class Login : Page
    {
        private readonly MainWindow mainWindow;
        private Updater.UpdateInfo? updateInfo;

        public Login()
        {
            InitializeComponent();
            PasswordBox.Focus();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            CheckForUpdate();
            Setup(true);
        }

        public Login(bool promptWinHello)
        {
            InitializeComponent();
            PasswordBox.Focus();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            CheckForUpdate();
            Setup(promptWinHello);
        }

        private async void CheckForUpdate()
        {
            if (InstallationContext.GetInstallationType() == InstallationType.MICROSOFT_STORE)
            {
                return;
            }
            updateInfo = await Updater.CheckForUpdate();
        }

        private async void Setup(bool promptWinHello)
        {
            PasswordBox.KeyDown += (sender, e) =>
            {
                if (Keyboard.IsKeyToggled(Key.CapsLock))
                {
                    ShowWarning("", I18n.GetString("login.warning.capslock.content"));
                }
                else
                {
                    if (InfoBar.Message == I18n.GetString("login.warning.capslock.content"))
                    {
                        InfoBar.IsOpen = false;
                    }
                }
            };
            try
            {
                await Auth.Init();
                Stats.TrackEvent(Stats.EventType.AppStarted);
                if (!Auth.IsLoginEnabled())
                {
                    PasswordBox.IsEnabled = false;
                    DisableButtons();
                    Auth.LoginInsecure();
                    OnLoggedIn();
                    return;
                }
                if (Auth.GetWebAuthnDevices().Count == 0)
                {
                    WebAuthnBtn.Visibility = Visibility.Collapsed;
                }
                if (Auth.IsWindowsHelloRegistered())
                {
                    if (promptWinHello)
                    {
                        DisableButtons();
                        LoginProgressBar.Visibility = Visibility.Visible;
                        await Auth.LoginWithWindowsHello();
                        OnLoggedIn();
                    }
                }
                else
                {
                    WinHelloButton.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                EnableButtons();
                LoginProgressBar.Visibility = Visibility.Hidden;
                if (ex.Message.Contains("UserCanceled"))
                {
                    return;
                }
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
            InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Error;
            InfoBar.IsOpen = true;
        }

        private void ShowWarning(string title, string message)
        {
            InfoBar.Title = title;
            InfoBar.Message = message;
            InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Warning;
            InfoBar.IsOpen = true;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ShowError("Error", I18n.GetString("welcome.pass.notempty"));
                return;
            }

            DisableButtons();
            LoginProgressBar.Visibility = Visibility.Visible;

            try
            {
                Auth.LoginWithPassword(Encoding.UTF8.GetBytes(PasswordBox.Password));
                OnLoggedIn();
            }
            catch (Exception ex)
            {
                LoginProgressBar.Visibility = Visibility.Collapsed;
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
            Database.Init();

            if (updateInfo != null)
            {
                mainWindow.FullContentFrame.Content = new UpdatePage(updateInfo);
                return;
            }
            mainWindow.FullContentFrame.Content = null;
            mainWindow.FullContentFrame.Visibility = Visibility.Collapsed;
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(Home));

            InactivityDetector.Start();
        }

        private async void WinHelloButton_Click(object sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;
            DisableButtons();
            LoginProgressBar.Visibility = Visibility.Visible;

            try
            {
                await Auth.LoginWithWindowsHello();
                OnLoggedIn();
            }
            catch (Exception ex)
            {
                LoginProgressBar.Visibility = Visibility.Collapsed;
                EnableButtons();

                if (ex.Message.Contains("UserCanceled"))
                {
                    return;
                }
                ShowError("Error", ex.Message);
            }
        }

        private void EnableButtons()
        {
            LoginButton.IsEnabled = true;
            WinHelloButton.IsEnabled = true;
            WebAuthnBtn.IsEnabled = true;
        }

        private void DisableButtons()
        {
            LoginButton.IsEnabled = false;
            WinHelloButton.IsEnabled = false;
            WebAuthnBtn.IsEnabled = false;
        }

        private async void WebAuthnBtn_Click(object sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;
            DisableButtons();
            LoginProgressBar.Visibility = Visibility.Visible;

            try
            {
                await Auth.LoginWithWebAuthn(mainWindow.GetWindowHandle());
                OnLoggedIn();
            }
            catch (Exception ex)
            {
                LoginProgressBar.Visibility = Visibility.Collapsed;
                EnableButtons();

                if (ex.Message.Contains("Canceled"))
                {
                    return;
                }
                ShowError("Error", ex.Message);
            }
        }
    }
}
