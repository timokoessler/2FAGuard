using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Guard.Core;
using Guard.Core.Installation;
using Guard.Core.Security;
using Guard.Core.Storage;

namespace Guard.Views.Pages.Start
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
            if (InstallationInfo.GetInstallationType() == InstallationType.MICROSOFT_STORE)
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
                if (!Auth.IsLoginEnabled())
                {
                    LoginButton.IsEnabled = false;
                    PasswordBox.IsEnabled = false;
                    Auth.LoginInsecure();
                    OnLoggedIn();
                    return;
                }
                if (Auth.IsWindowsHelloRegistered())
                {
                    if (promptWinHello)
                    {
                        LoginButton.IsEnabled = false;
                        WinHelloButton.IsEnabled = false;
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
                LoginButton.IsEnabled = true;
                WinHelloButton.IsEnabled = true;
                LoginProgressBar.Visibility = Visibility.Hidden;
                if (ex.Message.Contains("UserCanceled"))
                {
                    return;
                }
                Log.Logger.Error("Error during login setup: {0}", ex.Message);
                ShowEror("Error", ex.Message);
            }
        }

        private void ShowEror(string title, string message)
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
                ShowEror("Error", I18n.GetString("welcome.pass.notempty"));
                return;
            }

            LoginButton.IsEnabled = false;
            WinHelloButton.IsEnabled = false;
            LoginProgressBar.Visibility = Visibility.Visible;

            try
            {
                Auth.LoginWithPassword(PasswordBox.Password);
                OnLoggedIn();
            }
            catch (Exception ex)
            {
                LoginProgressBar.Visibility = Visibility.Collapsed;
                ShowEror("Error", ex.Message);
                LoginButton.IsEnabled = true;
                WinHelloButton.IsEnabled = true;
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
        }

        private async void WinHelloButton_Click(object sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;
            LoginButton.IsEnabled = false;
            WinHelloButton.IsEnabled = false;
            LoginProgressBar.Visibility = Visibility.Visible;

            try
            {
                await Auth.LoginWithWindowsHello();
                OnLoggedIn();
            }
            catch (Exception ex)
            {
                LoginProgressBar.Visibility = Visibility.Collapsed;
                LoginButton.IsEnabled = true;
                WinHelloButton.IsEnabled = true;

                if (ex.Message.Contains("UserCanceled"))
                {
                    return;
                }
                ShowEror("Error", ex.Message);
            }
        }
    }
}
