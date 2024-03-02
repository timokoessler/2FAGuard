﻿using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Security;

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