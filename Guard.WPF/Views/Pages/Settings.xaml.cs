﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Guard.Core;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Core.Security.WebAuthn;
using Guard.Core.Storage;
using Guard.WPF.Core;
using Guard.WPF.Core.Installation;
using Guard.WPF.Core.Security;
using Guard.WPF.Views.Dialogs;
using Guard.WPF.Views.Pages.Preferences;
using Wpf.Ui.Controls;

namespace Guard.WPF.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        private readonly MainWindow mainWindow;
        private bool ignoreWinHelloSwitchEvents = false;
        private bool ignoreAutoStartSwitchEvents = false;

        public Settings()
        {
            InitializeComponent();

            mainWindow = (MainWindow)Application.Current.MainWindow;

            AppCopyrightText.Text =
                $"Copyright © {DateTime.Now.Year} Timo Kössler and Open Source Contributors\n";
            AppVersionText.Text =
                $"Version {InstallationContext.GetVersionString()} ({InstallationContext.GetInstallationTypeString()})";

            SetSelectedTheme(SettingsManager.Settings.Theme);

            string[] supportedLanguages = Enum.GetNames(typeof(LanguageSetting));
            foreach (string lang in supportedLanguages)
            {
                LanguagesComboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = I18n.GetFullLanguageName(lang),
                        Tag = lang.ToString().ToLower(),
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

            if (InstallationContext.IsPortable())
            {
                AutoStartSwitch.IsEnabled = false;
            }
            else
            {
                SetAutostartStatus();
                AutoStartSwitch.Checked += AutoStartSwitch_Checked;
                AutoStartSwitch.Unchecked += AutoStartSwitch_Unchecked;
            }

            WinHelloSwitch.IsChecked = Auth.IsWindowsHelloRegistered();
            WinHelloSwitch.IsEnabled = Auth.IsLoginEnabled();
            WinHelloSwitch.Checked += (sender, e) => EnableWinHello();
            WinHelloSwitch.Unchecked += (sender, e) => DisableWinHello();

            CheckWindowsHelloAvailability();

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

            TraySwitch.IsChecked = SettingsManager.Settings.MinimizeToTray;

            TraySwitch.Checked += (sender, e) =>
            {
                SettingsManager.Settings.MinimizeToTray = true;
                mainWindow.AddTrayIcon();
                _ = SettingsManager.Save();
            };

            TraySwitch.Unchecked += (sender, e) =>
            {
                SettingsManager.Settings.MinimizeToTray = false;
                mainWindow.RemoveTrayIcon();
                _ = SettingsManager.Save();
            };

            RestoreWindowPlacementSwitch.IsChecked = SettingsManager
                .Settings
                .RestoreWindowPlacement;

            RestoreWindowPlacementSwitch.Checked += (sender, e) =>
            {
                SettingsManager.Settings.RestoreWindowPlacement = true;
                _ = SettingsManager.Save();
            };

            RestoreWindowPlacementSwitch.Unchecked += (sender, e) =>
            {
                SettingsManager.Settings.RestoreWindowPlacement = false;
                _ = SettingsManager.Save();
            };

            string[] lockTimes = Enum.GetNames(typeof(LockTimeSetting));
            foreach (string time in lockTimes)
            {
                string tag = time.ToString().ToLower();
                LockTimeComboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = I18n.GetString($"i.settings.locktime.{time}"),
                        Tag = tag,
                    }
                );
            }
            LockTimeComboBox.SelectedItem = LockTimeComboBox
                .Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x =>
                    ((string)x.Tag).Equals(
                        SettingsManager.Settings.LockTime.ToString(),
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            LockTimeComboBox.SelectionChanged += OnLockTimeSelectionChanged;

            WebAuthnBtn.IsEnabled = Auth.IsLoginEnabled();

            SetSelectedHideTokenSetting(SettingsManager.Settings.HideToken);
            HideTokenComboBox.SelectionChanged += OnHideTokenSelectionChanged;

            ApplyRegistrySettings();

            FixUniformGridSpacing(PersonalisationSettingsGrid, true);
            FixUniformGridSpacing(SecuritySettingsGrid, true);

            Core.EventManager.WindowSizeChanged += OnWindowSizeChanged;
            OnWindowSizeChanged(this, (mainWindow.ActualWidth, mainWindow.ActualHeight));

            Unloaded += (sender, e) =>
            {
                Core.EventManager.AppThemeChanged -= OnAppThemeChanged;
                Core.EventManager.WindowSizeChanged -= OnWindowSizeChanged;
            };
        }

        private void ApplyRegistrySettings()
        {
            (bool hideWinHello, bool hidePreventScreenRecording, bool hideSecurityKey) =
                RegistrySettings.GetSettingsPageOptions();

            if (hideWinHello)
            {
                WindowsHelloCard.Visibility = Visibility.Collapsed;
            }
            if (hidePreventScreenRecording)
            {
                PreventRecordingCard.Visibility = Visibility.Collapsed;
            }
            if (hideSecurityKey)
            {
                SecurityKeyCard.Visibility = Visibility.Collapsed;
            }
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

        private void SetSelectedHideTokenSetting(HideTokenSetting hideToken)
        {
            HideTokenComboBox.SelectedItem = HideTokenComboBox
                .Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x =>
                    ((string)x.Tag).Equals(hideToken.ToString(), StringComparison.OrdinalIgnoreCase)
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
                    mainWindow.UpdatePageTitle("settings");
                    if (Environment.OSVersion.Version.Build > 22621)
                    {
                        WindowBackdrop.RemoveBackground(mainWindow);
                    }
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

        private void OnLockTimeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LockTimeComboBox.SelectedItem != null)
            {
                string time = (string)((ComboBoxItem)LockTimeComboBox.SelectedItem).Tag;

                if (Enum.TryParse<LockTimeSetting>(time, true, out LockTimeSetting result))
                {
                    SettingsManager.Settings.LockTime = result;
                    _ = SettingsManager.Save();
                    if (result == LockTimeSetting.Never)
                    {
                        if (InactivityDetector.IsRunning())
                        {
                            InactivityDetector.Stop();
                        }
                    }
                    else
                    {
                        if (!InactivityDetector.IsRunning())
                        {
                            InactivityDetector.Start();
                        }
                    }
                }
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
                    MaxWidth = 400,
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
                    byte[] password = dialog.GetPassword();

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
                        MaxWidth = 400,
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
            mainWindow.Navigate(typeof(ChangePasswordPage), true);
        }

        private async void SetAutostartStatus()
        {
            if (InstallationContext.IsPortable())
            {
                return;
            }
            ignoreAutoStartSwitchEvents = true;
            AutoStartSwitch.IsEnabled = false;

            AutoStartSwitch.IsChecked = await Autostart.IsEnabled();

            ignoreAutoStartSwitchEvents = false;
            AutoStartSwitch.IsEnabled = true;
        }

        private async void AutoStartSwitch_Checked(object sender, RoutedEventArgs e)
        {
            if (ignoreAutoStartSwitchEvents)
            {
                return;
            }
            AutoStartSwitch.IsEnabled = false;
            try
            {
                await Autostart.Enable();
                AutoStartSwitch.IsEnabled = true;
            }
            catch (Exception ex)
            {
                AutoStartSwitch.IsEnabled = true;
                SetAutostartStatus();
                Log.Logger.Error("Error enabling autostart: {0}", ex.Message);

                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("settings.autostart.error.title"),
                    Content = ex.Message,
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400,
                }.ShowDialogAsync();
            }
        }

        private async void AutoStartSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ignoreAutoStartSwitchEvents)
            {
                return;
            }
            AutoStartSwitch.IsEnabled = false;
            try
            {
                await Autostart.Disable();
                AutoStartSwitch.IsEnabled = true;
            }
            catch (Exception ex)
            {
                AutoStartSwitch.IsEnabled = true;
                SetAutostartStatus();
                Log.Logger.Error("Error disabling autostart: {0}", ex.Message);

                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("settings.autostart.error.title"),
                    Content = ex.Message,
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400,
                }.ShowDialogAsync();
            }
        }

        private void License_Button_Click(object sender, RoutedEventArgs e)
        {
            _ = new Wpf.Ui.Controls.MessageBox
            {
                Title = "MIT License",
                Content =
                    @$"Copyright (c) 2024-{DateTime.Now.Year} Timo Kössler and Open Source Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.",
                CloseButtonText = I18n.GetString("dialog.close"),
                MaxWidth = 550,
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
Konscious.Security.Cryptography - Copyright Keef Aragon under MIT License
libsodium - Copyright Frank Denis under ISC License
LiteDB - Copyright Mauricio David under MIT License
NSec.Cryptography - Copyright Klaus Hartke under MIT License
Otp.NET - Copyright Kyle Spearrin under MIT License
Serilog - Copyright Serilog Contributors under Apache License 2.0
SharpVectors.Wpf - Copyright Elinam LLC under BSD 3-Clause License
Simple Icons - Copyright under CC0 1.0 Universal
Wpf.Ui - Copyright Leszek Pomianowski and WPF UI Contributors under MIT License
Wpf.Ui.Tray - Copyright Leszek Pomianowski and WPF UI Contributors under MIT License
ZXing.Net - Copyright Michael Jahn under Apache 2.0 License
ZXing.Net.Bindings.Windows.Compatibility - Copyright Michael Jahn under Apache 2.0 License
",
                CloseButtonText = I18n.GetString("dialog.close"),
                MaxWidth = 600,
            }.ShowDialogAsync();
        }

        private async void Reset_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Auth.IsLoginEnabled())
                {
                    var dialog = new PasswordDialog(mainWindow.GetRootContentDialogPresenter());
                    var result = await dialog.ShowAsync();

                    if (!result.Equals(ContentDialogResult.Primary))
                    {
                        return;
                    }

                    byte[] password = dialog.GetPassword();
                    if (!Auth.CheckPassword(password))
                    {
                        throw new Exception(I18n.GetString("passdialog.incorrect"));
                    }
                }

                var confirmResult = await new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("settings.reset.confirm.title"),
                    Content = I18n.GetString("settings.reset.confirm.content"),
                    CloseButtonText = I18n.GetString("dialog.cancel"),
                    PrimaryButtonText = I18n.GetString("settings.reset.confirm.btn"),
                    PrimaryButtonAppearance = ControlAppearance.Danger,
                    MaxWidth = 500,
                }.ShowDialogAsync();

                if (confirmResult == Wpf.Ui.Controls.MessageBoxResult.Primary)
                {
                    Reset.DeleteEverything();
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Error resetting app: {0} {1}", ex.Message, ex.StackTrace);
                _ = await new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("error"),
                    Content = ex.Message,
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 500,
                }.ShowDialogAsync();
            }
        }

        private void WebAuthn_Button_Click(object sender, RoutedEventArgs e)
        {
            if (!WebAuthnHelper.IsSupported())
            {
                _ = new Wpf.Ui.Controls.MessageBox
                {
                    Title = I18n.GetString("error"),
                    Content = I18n.GetString("settings.webauthn.notsupported"),
                    CloseButtonText = I18n.GetString("dialog.close"),
                    MaxWidth = 400,
                }.ShowDialogAsync();
                return;
            }

            mainWindow.Navigate(typeof(WebAuthnPage), true);
        }

        private async void CheckWindowsHelloAvailability()
        {
            if (!Auth.IsLoginEnabled())
            {
                // Switch already disabled
                return;
            }

            var supported = await WindowsHello.IsAvailable();
            if (!supported)
            {
                ignoreWinHelloSwitchEvents = true;
                WinHelloSwitch.IsEnabled = false;
                ignoreWinHelloSwitchEvents = false;
            }
        }

        private static void FixUniformGridSpacing(UniformGrid grid, bool multipleCols)
        {
            int i = 0;
            foreach (CardControl card in grid.Children)
            {
                if (card.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                if (multipleCols && i % 2 != 0)
                {
                    card.Margin = new Thickness(15, 0, 0, 15);
                }
                else
                {
                    card.Margin = new Thickness(0, 0, 0, 15);
                }
                ++i;
            }
        }

        private void OnHideTokenSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (HideTokenComboBox.SelectedItem != null)
            {
                string theme = (string)((ComboBoxItem)HideTokenComboBox.SelectedItem).Tag;

                if (Enum.TryParse(theme, true, out HideTokenSetting result))
                {
                    SettingsManager.Settings.HideToken = result;
                    _ = SettingsManager.Save();
                }
            }
        }

        private void OnWindowSizeChanged(object? sender, (double width, double height) size)
        {
            var onlyRows = size.width < 800;

            var newValue = onlyRows ? 1 : 2;

            if (PersonalisationSettingsGrid.Columns != newValue)
            {
                PersonalisationSettingsGrid.Columns = newValue;
                FixUniformGridSpacing(PersonalisationSettingsGrid, !onlyRows);
            }

            if (SecuritySettingsGrid.Columns != newValue)
            {
                SecuritySettingsGrid.Columns = newValue;
                FixUniformGridSpacing(SecuritySettingsGrid, !onlyRows);
            }

            if (onlyRows)
            {
                Grid.SetRow(AppVersionText, 1);
                Grid.SetColumn(AppVersionText, 0);
                AppVersionText.HorizontalAlignment = HorizontalAlignment.Left;
                AppVersionText.Margin = new Thickness(54, 5, 0, 0);
            }
            else
            {
                Grid.SetRow(AppVersionText, 0);
                Grid.SetColumn(AppVersionText, 1);
                AppVersionText.HorizontalAlignment = HorizontalAlignment.Right;
                AppVersionText.Margin = new Thickness(0, 0, 16, 0);
            }

            LinksStackPanel.Orientation = onlyRows ? Orientation.Vertical : Orientation.Horizontal;
        }
    }
}
