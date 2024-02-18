using System.Windows;
using System.Windows.Controls;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Core.Security;

namespace TOTPTokenGuard.Views.Pages.Start
{
    /// <summary>
    /// Interaktionslogik für Login.xaml
    /// </summary>
    public partial class Login : Page
    {
        private MainWindow mainWindow;

        public Login()
        {
            InitializeComponent();
            PasswordBox.Focus();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            _ = Setup();
        }

        private async Task Setup()
        {
            try
            {
                await Auth.Init();
                if (Auth.IsWindowsHelloRegistered())
                {
                    await Auth.LoginWithWindowsHello();
                    OnLoggedIn();
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("UserCanceled"))
                {
                    return;
                }
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

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ShowEror("Error", I18n.GetString("welcome.pass.notempty"));
                return;
            }

            LoginButton.IsEnabled = false;
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
            }
        }

        private void OnLoggedIn()
        {
            if (!Auth.IsLoggedIn())
            {
                return;
            }
            mainWindow.FullContentFrame.Content = null;
            mainWindow.FullContentFrame.Visibility = Visibility.Collapsed;
            mainWindow.ShowNavigation();
            mainWindow.Navigate(typeof(Home));
        }
    }
}
