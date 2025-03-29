using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Guard.Core.Security;
using Guard.WPF.Core;
using Guard.WPF.Core.Security;

namespace Guard.WPF.Views.Pages.Preferences
{
    /// <summary>
    /// Interaktionslogik für ChangePasswordPage.xaml
    /// </summary>
    public partial class ChangePasswordPage : Page
    {
        private readonly MainWindow mainWindow;
        private readonly bool insecure = false;

        public ChangePasswordPage()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            insecure = !Auth.IsLoginEnabled();
            if (insecure)
            {
                CurrentPass.Visibility = Visibility.Collapsed;
                PasswordBox.Focus();
            }
            else
            {
                CurrentPass.Focus();
            }

            CurrentPass.KeyDown += (sender, e) => CapsLockWarning();
            PasswordBox.KeyDown += (sender, e) => CapsLockWarning();
            PasswordBoxRepeat.KeyDown += (sender, e) => CapsLockWarning();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ShowEror("Error", I18n.GetString("welcome.pass.notempty"));
                return;
            }
            if (!insecure && string.IsNullOrWhiteSpace(CurrentPass.Password))
            {
                ShowEror("Error", I18n.GetString("changepass.current.notempty"));
                return;
            }

            (bool passValid, string? passInvalidReason) = PasswordComplexity.CheckPassword(
                PasswordBox.Password
            );

            if (!passValid)
            {
                ShowEror("Error", passInvalidReason ?? "Invalid Password");
                return;
            }

            if (PasswordBox.Password != PasswordBoxRepeat.Password)
            {
                ShowEror("Error", I18n.GetString("welcome.pass.notmatch"));
                return;
            }

            SaveButton.IsEnabled = false;
            RegisterProgressBar.Visibility = Visibility.Visible;

            try
            {
                if (!insecure && !Auth.CheckPassword(Encoding.UTF8.GetBytes(CurrentPass.Password)))
                {
                    throw new Exception(I18n.GetString("changepass.current.incorrect"));
                }
                await Auth.ChangePassword(Encoding.UTF8.GetBytes(PasswordBox.Password));
                mainWindow.Navigate(typeof(Settings));
            }
            catch (Exception ex)
            {
                RegisterProgressBar.Visibility = Visibility.Collapsed;
                ShowEror("Error", ex.Message);
                SaveButton.IsEnabled = true;
            }
        }

        private void ShowEror(string title, string message)
        {
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

        private void CapsLockWarning()
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
        }
    }
}
