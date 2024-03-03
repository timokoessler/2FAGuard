using System.Windows;
using System.Windows.Controls;
using Guard.Core;
using Guard.Core.Installation;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Core.Storage;
using Guard.Views.Controls;
using Wpf.Ui.Controls;

namespace Guard.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        private readonly MainWindow mainWindow;
        private bool ignoreWinHelloSwitchEvents = false;

        public Settings()
        {
            InitializeComponent();

            mainWindow = (MainWindow)Application.Current.MainWindow;

            AppCopyrightText.Text =
                $"Copyright © {DateTime.Now.Year} Timo Kössler and Open Source Contributors\n";
            AppVersionText.Text = $"Version {InstallationInfo.GetVersionString()}";

            SetSelectedTheme(SettingsManager.Settings.Theme);

            string[] supportedLanguages = Enum.GetNames(typeof(LanguageSetting));
            foreach (string lang in supportedLanguages)
            {
                LanguagesComboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = I18n.GetFullLanguageName(lang),
                        Tag = lang.ToString().ToLower()
                    }
                );
            }

            SetSelectedLanguage(
                SettingsManager.Settings.Language == LanguageSetting.System
                    ? LanguageSetting.System
                    : SettingsManager.Settings.Language
            );

            LanguagesComboBox.SelectionChanged += OnLanguageSelectionChanged;
            ThemeComboBox.SelectionChanged += OnThemeSelectionChanged;

            WinHelloSwitch.IsChecked = Auth.IsWindowsHelloRegistered();
            WinHelloSwitch.IsEnabled = Auth.IsLoginEnabled();
            WinHelloSwitch.Checked += (sender, e) => EnableWinHello();
            WinHelloSwitch.Unchecked += (sender, e) => DisableWinHello();

            ScreenRecordingSwitch.IsChecked = SettingsManager.Settings.PreventRecording;
            ScreenRecordingSwitch.Checked += (sender, e) =>
            {
                mainWindow.AllowScreenRecording(false);
                SettingsManager.Settings.PreventRecording = true;
                _ = SettingsManager.Save();
            };
            ScreenRecordingSwitch.Unchecked += (sender, e) =>
            {
                mainWindow.AllowScreenRecording(true);
                SettingsManager.Settings.PreventRecording = false;
                _ = SettingsManager.Save();
            };

            ScreenLockSwitch.IsChecked = SettingsManager.Settings.LockOnScreenLock;
            ScreenLockSwitch.Checked += (sender, e) =>
            {
                SettingsManager.Settings.LockOnScreenLock = true;
                _ = SettingsManager.Save();
            };
            ScreenLockSwitch.Unchecked += (sender, e) =>
            {
                SettingsManager.Settings.LockOnScreenLock = false;
                _ = SettingsManager.Save();
            };

            Core.EventManager.AppThemeChanged += OnAppThemeChanged;
        }

        private void SetSelectedLanguage(LanguageSetting lang)
        {
            LanguagesComboBox.SelectedItem = LanguagesComboBox
                .Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x =>
                    ((string)x.Tag).Equals(lang.ToString(), StringComparison.OrdinalIgnoreCase)
                );
        }

        private void SetSelectedTheme(ThemeSetting theme)
        {
            ThemeComboBox.SelectedItem = ThemeComboBox
                .Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x =>
                    ((string)x.Tag).Equals(theme.ToString(), StringComparison.OrdinalIgnoreCase)
                );
        }

        private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguagesComboBox.SelectedItem != null)
            {
                string lang = (string)((ComboBoxItem)LanguagesComboBox.SelectedItem).Tag;

                if (Enum.TryParse<LanguageSetting>(lang, true, out LanguageSetting result))
                {
                    I18n.ChangeLanguage(result);
                    mainWindow.UpdatePageTitle();
                }
            }
        }

        private void OnThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem != null)
            {
                string theme = (string)((ComboBoxItem)ThemeComboBox.SelectedItem).Tag;

                if (Enum.TryParse<ThemeSetting>(theme, true, out ThemeSetting result))
                {
                    mainWindow.ApplyTheme(result);
                    SettingsManager.Settings.Theme = result;
                    Core.EventManager.EmitAppThemeChanged(
                        result,
                        Core.EventManager.AppThemeChangedEventArgs.EventSource.Settings
                    );
                    _ = SettingsManager.Save();
                }
            }
        }

        private void OnAppThemeChanged(object? sender, Core.EventManager.AppThemeChangedEventArgs e)
        {
            if (e.Source != Core.EventManager.AppThemeChangedEventArgs.EventSource.Settings)
            {
                SetSelectedTheme(e.Theme);
            }
        }

        private async void EnableWinHello()
        {
            if (ignoreWinHelloSwitchEvents)
            {
                return;
            }
            ignoreWinHelloSwitchEvents = true;
            WinHelloSwitch.IsEnabled = false;
            try
            {
                await Auth.RegisterWindowsHello();
                await Auth.SaveFile();
                WinHelloSwitch.IsEnabled = true;
                ignoreWinHelloSwitchEvents = false;
            }
            catch (Exception ex)
            {
                WinHelloSwitch.IsChecked = false;
                WinHelloSwitch.IsEnabled = true;
                ignoreWinHelloSwitchEvents = false;
                if (ex.Message.Contains("UserCanceled"))
                {
                    return;
                }
                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("settings.winhello.failed.title"),
                    Content = $"{I18n.GetString("settings.winhello.failed.content")} {ex.Message}",
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400
                }.ShowDialogAsync();
            }
        }

        private async void DisableWinHello()
        {
            if (ignoreWinHelloSwitchEvents)
            {
                return;
            }
            ignoreWinHelloSwitchEvents = true;
            var dialog = new PasswordDialog(mainWindow.GetRootContentDialogPresenter());
            var result = await dialog.ShowAsync();

            if (result.Equals(ContentDialogResult.Primary))
            {
                try
                {
                    string password = dialog.GetPassword();

                    if (!Auth.CheckPassword(password))
                    {
                        throw new Exception(I18n.GetString("passdialog.incorrect"));
                    }

                    Auth.UnregisterWindowsHello();
                    await Auth.SaveFile();
                    ignoreWinHelloSwitchEvents = false;
                }
                catch (Exception ex)
                {
                    WinHelloSwitch.IsChecked = true;
                    ignoreWinHelloSwitchEvents = false;
                    _ = new Wpf.Ui.Controls.MessageBox
                    {
                        Title = I18n.GetString("settings.winhello.failed.title"),
                        Content =
                            $"{I18n.GetString("settings.winhello.failed.content")} {ex.Message}",
                        CloseButtonText = I18n.GetString("dialog.close"),
                        MaxWidth = 400
                    }.ShowDialogAsync();
                }
            }
            else
            {
                WinHelloSwitch.IsChecked = true;
                ignoreWinHelloSwitchEvents = false;
            }
        }

        private void Change_Pass_Button_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Navigate(typeof(ChangePasswordPage));
        }
    }
}
