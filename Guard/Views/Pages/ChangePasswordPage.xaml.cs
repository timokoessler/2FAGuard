using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Security;

namespace Guard.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für ChangePasswordPage.xaml
    /// </summary>
    public partial class ChangePasswordPage : Page
    {

        private MainWindow mainWindow;
        private readonly bool insecure = false;
        public ChangePasswordPage()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            insecure = !Auth.IsLoginEnabled();
            if(insecure)
            {
                CurrentPass.Visibility = Visibility.Collapsed;
                PasswordBox.Focus();
            } else {
                CurrentPass.Focus();
            }
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
            if (PasswordBox.Password.Length < 8 || PasswordBox.Password.Length > 128)
            {
                ShowEror("Error", I18n.GetString("welcome.pass.length"));
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
                if(!insecure && !Auth.CheckPassword(CurrentPass.Password))
                {
                    throw new Exception(I18n.GetString("changepass.current.incorrect"));
                }
                await Auth.ChangePassword(PasswordBox.Password);
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
    }
}
