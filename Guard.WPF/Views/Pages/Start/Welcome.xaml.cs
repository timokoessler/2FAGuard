﻿using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Security;
using Guard.WPF.Core;

namespace Guard.WPF.Views.Pages.Start
{
    /// <summary>
    /// Interaktionslogik für Welcome.xaml
    /// </summary>
    public partial class Welcome : Page
    {
        private readonly MainWindow mainWindow;

        public Welcome()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.HideNavigation();

            Loaded += (sender, e) => ApplyRegistrySettings();

            Core.EventManager.WindowSizeChanged += OnWindowSizeChanged;

            Unloaded += (object? sender, RoutedEventArgs e) =>
            {
                Core.EventManager.WindowSizeChanged -= OnWindowSizeChanged;
            };

            OnWindowSizeChanged(null, (mainWindow.ActualWidth, mainWindow.ActualHeight));
        }

        private void ApplyRegistrySettings()
        {
            if (mainWindow.SkipApplyRegistrySettings)
            {
                mainWindow.SkipApplyRegistrySettings = false;
                return;
            }
            (bool hideSkip, bool hideWinHello, bool hidePassword) =
                RegistrySettings.GetSetupHideOptions();

            if (hideSkip)
            {
                SkipBtn.Visibility = Visibility.Collapsed;
            }
            if (hideWinHello)
            {
                WinHelloBtn.Visibility = Visibility.Collapsed;
            }
            if (hidePassword)
            {
                PasswordBtn.Visibility = Visibility.Collapsed;
            }

            if (hideSkip && hidePassword)
            {
                SetupWithWinHello();
            }
            if (hideSkip && hideWinHello)
            {
                SetupWithPassword();
            }
            if (hideWinHello && hidePassword)
            {
                SetupInsecure(false);
            }
        }

        private void CardAction_WinHello_Click(object sender, RoutedEventArgs e)
        {
            SetupWithWinHello();
        }

        private async void SetupWithWinHello()
        {
            if (!await WindowsHello.IsAvailable())
            {
                _ = await new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("welcome.hello.notavailable"),
                    Content = I18n.GetString("welcome.hello.notavailable.content"),
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400,
                }.ShowDialogAsync();
                return;
            }
            if (InstallationContext.IsPortable())
            {
                Wpf.Ui.Controls.MessageBoxResult sucessDialogResult =
                    await new Wpf.Ui.Controls.MessageBox
                    {
                        Title = I18n.GetString("welcome.portable.winhello.title"),
                        Content = I18n.GetString("welcome.portable.winhello.content"),
                        CloseButtonText = I18n.GetString("dialog.close"),
                        PrimaryButtonText = I18n.GetString("dialog.next"),
                        MaxWidth = 500,
                    }.ShowDialogAsync();

                if (sucessDialogResult != Wpf.Ui.Controls.MessageBoxResult.Primary)
                {
                    return;
                }
            }
            mainWindow.FullContentFrame.Content = new SetupPassword(true);
        }

        private void CardAction_Password_Click(object sender, RoutedEventArgs e)
        {
            SetupWithPassword();
        }

        private void SetupWithPassword()
        {
            mainWindow.FullContentFrame.Content = new SetupPassword(false);
        }

        private void Button_Skip_Click(object sender, RoutedEventArgs e)
        {
            SetupInsecure();
        }

        private async void SetupInsecure(bool showWarning = true)
        {
            if (showWarning)
            {
                Wpf.Ui.Controls.MessageBoxResult sucessDialogResult =
                    await new Wpf.Ui.Controls.MessageBox
                    {
                        Title = I18n.GetString("welcome.insecure.dialog.title"),
                        Content = I18n.GetString("welcome.insecure.dialog.content"),
                        CloseButtonText = I18n.GetString("dialog.close"),
                        PrimaryButtonText = I18n.GetString("welcome.insecure.dialog.continue"),
                        MaxWidth = 500,
                    }.ShowDialogAsync();

                if (sucessDialogResult != Wpf.Ui.Controls.MessageBoxResult.Primary)
                {
                    return;
                }
            }

            try
            {
                await Auth.Init();
                await Auth.RegisterInsecure();
                mainWindow.FullContentFrame.Content = new SetupCompleted();
            }
            catch (Exception ex)
            {
                _ = await new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Error",
                    Content = ex.Message,
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 500,
                }.ShowDialogAsync();
            }
        }

        private void OnWindowSizeChanged(object? sender, (double width, double height) size)
        {
            if (mainWindow.ActualHeight < 600)
            {
                HeaderLogo.Visibility = Visibility.Collapsed;
                HeaderTitle.Visibility = Visibility.Collapsed;
                var margin = HeaderSubtitle.Margin;
                margin.Top = 60;
                HeaderSubtitle.Margin = margin;
            }
            else
            {
                HeaderLogo.Visibility = Visibility.Visible;
                HeaderTitle.Visibility = Visibility.Visible;
                var margin = HeaderSubtitle.Margin;
                margin.Top = 20;
                HeaderSubtitle.Margin = margin;
            }
        }
    }
}
