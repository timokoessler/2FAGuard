using Guard.Core;
using Guard.Core.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Guard.Views.Pages.Start
{
    /// <summary>
    /// Interaktionslogik für SetupPassword.xaml
    /// </summary>
    public partial class SetupPassword : Page
    {
        private readonly bool enableWinHello;
        private readonly MainWindow mainWindow;

        public SetupPassword(bool enableWinHello)
        {
            this.enableWinHello = enableWinHello;
            InitializeComponent();
            PasswordBox.Focus();
            mainWindow = (MainWindow)Application.Current.MainWindow;

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
                await Auth.Init();
                await Auth.Register(PasswordBox.Password, enableWinHello);
                mainWindow.FullContentFrame.Content = new SetupCompleted();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Error during registration: {0} {1}", ex.Message, ex.StackTrace);
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
