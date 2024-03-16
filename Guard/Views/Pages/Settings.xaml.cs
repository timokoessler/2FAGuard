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
            AppVersionText.Text =
                $"Version {InstallationInfo.GetVersionString()} ({InstallationInfo.GetInstallationTypeString()})";

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
            AutoStartSwitch.IsChecked = Autostart.IsEnabled();

            AutoStartSwitch.Checked += (sender, e) => Autostart.Enable();
            AutoStartSwitch.Unchecked += (sender, e) => Autostart.Disable();

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

            Unloaded += (sender, e) =>
            {
                Core.EventManager.AppThemeChanged -= OnAppThemeChanged;
            };
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

                if (Enum.TryParse(theme, true, out ThemeSetting result))
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

        private void License_Button_Click(object sender, RoutedEventArgs e)
        {
            _ = new Wpf.Ui.Controls.MessageBox
            {
                Title = "MIT License",
                Content =
                    @"Copyright (c) 2024 Timo Kössler and Open Source Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.",
                CloseButtonText = I18n.GetString("dialog.close"),
                MaxWidth = 550
            }.ShowDialogAsync();
        }

        private void OSS_Button_Click(object sender, RoutedEventArgs e)
        {
            _ = new Wpf.Ui.Controls.MessageBox
            {
                Title = I18n.GetString("settings.oss"),
                Content =
                    @"Google.Protobuf - Copyright Google Inc. under BSD-3-Clause License
GuerrillaNtp - Copyright Robert Važan and contributors under Apache License 2.0
LiteDB - Copyright Mauricio David under MIT License
NSec.Cryptography - Copyright Klaus Hartke under MIT License
Otp.NET - Copyright Kyle Spearrin under MIT License
SharpVectors.Wpf - Copyright Elinam LLC under BSD 3-Clause License
Simple Icons - Copyright under CC0 1.0 Universal
Wpf.Ui - Copyright Leszek Pomianowski and WPF UI Contributors under MIT License
ZXing.Net - Copytight Michael Jahn under Apache 2.0 License
",
                CloseButtonText = I18n.GetString("dialog.close"),
                MaxWidth = 600
            }.ShowDialogAsync();
        }
    }
}
