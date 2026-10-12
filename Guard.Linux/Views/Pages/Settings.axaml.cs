using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Guard.Core;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Core.Security.Linux;
using Guard.Core.Storage;
using Guard.Linux.Core;
using Guard.WPF.Core.Installation;
using Guard.Linux.Core.Security;
using Guard.Linux.Views.Pages.Preferences;
using Guard.WPF.Core;

namespace Guard.Linux.Views.Pages
{
    public partial class Settings : UserControl
    {
        private readonly MainWindow mainWindow;
        private bool ignoreKeyringSwitchEvents;
        private bool ignoreAutoStartSwitchEvents;
        private bool ignoreSelectionEvents;

        public Settings()
        {
            InitializeComponent();
            mainWindow = App.MainWindow!;

            AppCopyrightText.Text =
                $"Copyright © {DateTime.Now.Year} Timo Kössler and Open Source Contributors";
            AppVersionText.Text =
                $"Version {InstallationContext.GetVersionString()} ({InstallationContext.GetInstallationTypeString()})";

            InitAppearanceSettings();
            InitWindowSettings();
            InitSecuritySettings();
            InitTokenSettings();
            ApplyRegistrySettings();

            Loaded += (_, _) =>
            {
                WPF.Core.EventManager.AppThemeChanged += OnAppThemeChanged;
                WPF.Core.EventManager.WindowSizeChanged += OnWindowSizeChanged;
                OnWindowSizeChanged(this, (mainWindow.Bounds.Width, mainWindow.Bounds.Height));
            };
            Unloaded += (_, _) =>
            {
                WPF.Core.EventManager.AppThemeChanged -= OnAppThemeChanged;
                WPF.Core.EventManager.WindowSizeChanged -= OnWindowSizeChanged;
            };
        }

        private void InitAppearanceSettings()
        {
            SelectByTag(ThemeComboBox, SettingsManager.Settings.Theme.ToString());
            ThemeComboBox.SelectionChanged += OnThemeSelectionChanged;

            foreach (string lang in Enum.GetNames<LanguageSetting>())
            {
                LanguagesComboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = I18n.GetFullLanguageName(lang),
                        Tag = lang.ToLowerInvariant(),
                    }
                );
            }
            SelectByTag(LanguagesComboBox, SettingsManager.Settings.Language.ToString());
            LanguagesComboBox.SelectionChanged += OnLanguageSelectionChanged;
        }

        private void InitWindowSettings()
        {
            if (InstallationContext.IsPortable())
            {
                AutoStartSwitch.IsEnabled = false;
            }
            else
            {
                SetAutostartStatus();
                AutoStartSwitch.IsCheckedChanged += OnAutoStartSwitchChanged;
            }

            TraySwitch.IsChecked = SettingsManager.Settings.MinimizeToTray;
            TraySwitch.IsCheckedChanged += (_, _) =>
            {
                bool enabled = TraySwitch.IsChecked == true;
                SettingsManager.Settings.MinimizeToTray = enabled;
                if (enabled)
                {
                    mainWindow.AddTrayIcon();
                }
                else
                {
                    mainWindow.RemoveTrayIcon();
                }
                _ = SettingsManager.Save();
            };

            RestoreWindowPlacementSwitch.IsChecked = SettingsManager.Settings.RestoreWindowPlacement;
            RestoreWindowPlacementSwitch.IsCheckedChanged += (_, _) =>
            {
                SettingsManager.Settings.RestoreWindowPlacement =
                    RestoreWindowPlacementSwitch.IsChecked == true;
                _ = SettingsManager.Save();
            };
        }

        private void InitSecuritySettings()
        {
            ignoreKeyringSwitchEvents = true;
            KeyringSwitch.IsChecked = Auth.IsKeyringUnlockRegistered();
            KeyringSwitch.IsEnabled = false;
            ignoreKeyringSwitchEvents = false;
            KeyringSwitch.IsCheckedChanged += OnKeyringSwitchChanged;
            CheckKeyringAvailability();

            bool forceLockOnScreenLock = RegistrySettings.ForceLockOnScreenLock();
            ScreenLockSwitch.IsEnabled = !forceLockOnScreenLock;
            ScreenLockSwitch.IsChecked =
                SettingsManager.Settings.LockOnScreenLock || forceLockOnScreenLock;
            ScreenLockSwitch.IsCheckedChanged += (_, _) =>
            {
                SettingsManager.Settings.LockOnScreenLock = ScreenLockSwitch.IsChecked == true;
                _ = SettingsManager.Save();
            };

            AddEnumItems<LockTimeSetting>(LockTimeComboBox, "i.settings.locktime.");
            LockTimeSetting? forcedLockTime = RegistrySettings.GetForcedLockTime();
            SelectByTag(LockTimeComboBox, (forcedLockTime ?? SettingsManager.Settings.LockTime).ToString());
            LockTimeComboBox.IsEnabled = forcedLockTime == null;
            LockTimeComboBox.SelectionChanged += OnLockTimeSelectionChanged;

            WebAuthnBtn.IsEnabled = Auth.IsLoginEnabled();
        }

        private void InitTokenSettings()
        {
            SelectByTag(HideTokenComboBox, SettingsManager.Settings.HideToken.ToString());
            OnEnumSelectionChanged<HideTokenSetting>(
                HideTokenComboBox,
                value => SettingsManager.Settings.HideToken = value
            );

            AddEnumItems<ClearClipboardSetting>(ClearClipboardComboBox, "i.settings.clearclipboard.");
            ClearClipboardSetting? forcedClearClipboard = RegistrySettings.GetForcedClearClipboard();
            SelectByTag(
                ClearClipboardComboBox,
                (forcedClearClipboard ?? SettingsManager.Settings.ClearClipboard).ToString()
            );
            ClearClipboardComboBox.IsEnabled = forcedClearClipboard == null;
            OnEnumSelectionChanged<ClearClipboardSetting>(
                ClearClipboardComboBox,
                value => SettingsManager.Settings.ClearClipboard = value
            );
        }

        private static void AddEnumItems<T>(ComboBox comboBox, string keyPrefix)
            where T : struct, Enum
        {
            foreach (string name in Enum.GetNames<T>())
            {
                comboBox.Items.Add(
                    new ComboBoxItem
                    {
                        Content = I18n.GetString(keyPrefix + name),
                        Tag = name.ToLowerInvariant(),
                    }
                );
            }
        }

        private void OnEnumSelectionChanged<T>(ComboBox comboBox, Action<T> apply)
            where T : struct, Enum
        {
            comboBox.SelectionChanged += (_, _) =>
            {
                if (
                    !ignoreSelectionEvents
                    && GetSelectedTag(comboBox) is string tag
                    && Enum.TryParse(tag, true, out T result)
                )
                {
                    apply(result);
                    _ = SettingsManager.Save();
                }
            };
        }

        private static void SelectByTag(ComboBox comboBox, string tag)
        {
            comboBox.SelectedItem = comboBox
                .Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x => ((string)x.Tag!).Equals(tag, StringComparison.OrdinalIgnoreCase));
        }

        private static string? GetSelectedTag(ComboBox comboBox) =>
            (comboBox.SelectedItem as ComboBoxItem)?.Tag as string;

        private void ApplyRegistrySettings()
        {
            (bool hideKeyring, _, bool hideSecurityKey) = RegistrySettings.GetSettingsPageOptions();
            KeyringCard.IsVisible = !hideKeyring;
            SecurityKeyCard.IsVisible = !hideSecurityKey;
        }

        private void OnLanguageSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (
                !ignoreSelectionEvents
                && GetSelectedTag(LanguagesComboBox) is string lang
                && Enum.TryParse(lang, true, out LanguageSetting result)
            )
            {
                I18n.ChangeLanguage(result);
                mainWindow.UpdatePageTitle("settings");
                RefreshLocalizedComboBoxes();
            }
        }

        // Items created in code don't use dynamic resources and the combo boxes keep showing
        // the text of the selected item from before the change, so both are refreshed manually
        private void RefreshLocalizedComboBoxes()
        {
            foreach (ComboBoxItem item in LockTimeComboBox.Items.OfType<ComboBoxItem>())
            {
                string name = Enum.GetNames<LockTimeSetting>()
                    .First(n => n.Equals((string)item.Tag!, StringComparison.OrdinalIgnoreCase));
                item.Content = I18n.GetString($"i.settings.locktime.{name}");
            }
            foreach (ComboBoxItem item in ClearClipboardComboBox.Items.OfType<ComboBoxItem>())
            {
                string name = Enum.GetNames<ClearClipboardSetting>()
                    .First(n => n.Equals((string)item.Tag!, StringComparison.OrdinalIgnoreCase));
                item.Content = I18n.GetString($"i.settings.clearclipboard.{name}");
            }

            // Resource changes are propagated asynchronously
            Dispatcher.UIThread.Post(
                () =>
                {
                    ignoreSelectionEvents = true;
                    foreach (
                        ComboBox comboBox in new[]
                        {
                            ThemeComboBox,
                            LanguagesComboBox,
                            HideTokenComboBox,
                            LockTimeComboBox,
                            ClearClipboardComboBox,
                        }
                    )
                    {
                        int index = comboBox.SelectedIndex;
                        comboBox.SelectedIndex = -1;
                        comboBox.SelectedIndex = index;
                    }
                    ignoreSelectionEvents = false;
                },
                DispatcherPriority.Background
            );
        }

        private void OnThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (ignoreSelectionEvents)
            {
                return;
            }
            if (
                GetSelectedTag(ThemeComboBox) is string theme
                && Enum.TryParse(theme, true, out ThemeSetting result)
            )
            {
                MainWindow.ApplyTheme(result);
                SettingsManager.Settings.Theme = result;
                WPF.Core.EventManager.EmitAppThemeChanged(
                    result,
                    WPF.Core.EventManager.AppThemeChangedEventArgs.EventSource.Settings
                );
                _ = SettingsManager.Save();
            }
        }

        private void OnAppThemeChanged(object? sender, WPF.Core.EventManager.AppThemeChangedEventArgs e)
        {
            if (e.Source != WPF.Core.EventManager.AppThemeChangedEventArgs.EventSource.Settings)
            {
                ignoreSelectionEvents = true;
                SelectByTag(ThemeComboBox, e.Theme.ToString());
                ignoreSelectionEvents = false;
            }
        }

        private void OnLockTimeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (
                ignoreSelectionEvents
                || GetSelectedTag(LockTimeComboBox) is not string time
                || !Enum.TryParse(time, true, out LockTimeSetting result)
            )
            {
                return;
            }
            SettingsManager.Settings.LockTime = result;
            _ = SettingsManager.Save();
            if (result == LockTimeSetting.Never)
            {
                if (InactivityDetector.IsRunning())
                {
                    InactivityDetector.Stop();
                }
            }
            else if (!InactivityDetector.IsRunning())
            {
                InactivityDetector.Start();
            }
        }

        private async void CheckKeyringAvailability()
        {
            if (!Auth.IsLoginEnabled())
            {
                return;
            }
            bool available = await Task.Run(Auth.IsKeyringUnlockAvailable);
            // Allow disabling the keyring unlock even if the keyring is not reachable right now
            KeyringSwitch.IsEnabled = available || Auth.IsKeyringUnlockRegistered();
        }

        private void SetKeyringSwitch(bool value)
        {
            ignoreKeyringSwitchEvents = true;
            KeyringSwitch.IsChecked = value;
            ignoreKeyringSwitchEvents = false;
        }

        private void OnKeyringSwitchChanged(object? sender, RoutedEventArgs e)
        {
            if (ignoreKeyringSwitchEvents)
            {
                return;
            }
            if (KeyringSwitch.IsChecked == true)
            {
                EnableKeyring();
            }
            else
            {
                DisableKeyring();
            }
        }

        private async void EnableKeyring()
        {
            KeyringSwitch.IsEnabled = false;
            try
            {
                await Auth.RegisterKeyringUnlock();
                await Auth.SaveFile();
            }
            catch (Exception ex)
            {
                SetKeyringSwitch(false);
                if (ex is OperationCanceledException || ex.Message.Contains("UserCanceled"))
                {
                    return;
                }
                Log.Logger.Error("Failed to enable keyring unlock: {0}", ex.Message);
                await Dialogs.MessageBox(
                    I18n.GetString("settings.winhello.failed.title"),
                    $"{I18n.GetString("settings.winhello.failed.content")} {ex.Message}"
                );
            }
            finally
            {
                KeyringSwitch.IsEnabled = true;
            }
        }

        private async void DisableKeyring()
        {
            KeyringSwitch.IsEnabled = false;
            try
            {
                byte[]? password = await Dialogs.PasswordDialog();
                if (password == null)
                {
                    SetKeyringSwitch(true);
                    return;
                }
                if (!Auth.CheckPassword(password))
                {
                    throw new Exception(I18n.GetString("passdialog.incorrect"));
                }
                Auth.UnregisterKeyringUnlock();
                await Auth.SaveFile();
            }
            catch (Exception ex)
            {
                SetKeyringSwitch(true);
                await Dialogs.MessageBox(
                    I18n.GetString("settings.winhello.failed.title"),
                    $"{I18n.GetString("settings.winhello.failed.content")} {ex.Message}"
                );
            }
            finally
            {
                KeyringSwitch.IsEnabled = true;
            }
        }

        private void Change_Pass_Button_Click(object? sender, RoutedEventArgs e)
        {
            mainWindow.Navigate(typeof(ChangePasswordPage), true);
        }

        private async void SetAutostartStatus()
        {
            ignoreAutoStartSwitchEvents = true;
            AutoStartSwitch.IsEnabled = false;
            AutoStartSwitch.IsChecked = await Autostart.IsEnabled();
            AutoStartSwitch.IsEnabled = true;
            ignoreAutoStartSwitchEvents = false;
        }

        private async void OnAutoStartSwitchChanged(object? sender, RoutedEventArgs e)
        {
            if (ignoreAutoStartSwitchEvents)
            {
                return;
            }
            bool enable = AutoStartSwitch.IsChecked == true;
            AutoStartSwitch.IsEnabled = false;
            try
            {
                if (enable)
                {
                    await Autostart.Enable();
                }
                else
                {
                    await Autostart.Disable();
                }
                AutoStartSwitch.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Error changing autostart: {0}", ex.Message);
                SetAutostartStatus();
                await Dialogs.MessageBox(I18n.GetString("settings.autostart.error.title"), ex.Message);
            }
        }

        private void Link_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Control { Tag: string url })
            {
                ExceptionHandler.OpenUrl(url);
            }
        }

        private async void License_Button_Click(object? sender, RoutedEventArgs e)
        {
            await Dialogs.MessageBox(
                "MIT License",
                $@"Copyright (c) 2024-{DateTime.Now.Year} Timo Kössler and Open Source Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.",
                maxWidth: 550
            );
        }

        private async void OSS_Button_Click(object? sender, RoutedEventArgs e)
        {
            await Dialogs.MessageBox(
                I18n.GetString("settings.oss"),
                @"Avalonia - Copyright .NET Foundation and Contributors under MIT License
Avalonia.Svg.Skia - Copyright Wiesław Šoltés under MIT License
FluentAvaloniaUI - Copyright amwx under MIT License
Fluent UI System Icons - Copyright Microsoft Corporation under MIT License
Google.Protobuf - Copyright Google Inc. under BSD-3-Clause License
GuerrillaNtp - Copyright Robert Važan and contributors under Apache License 2.0
Konscious.Security.Cryptography - Copyright Keef Aragon under MIT License
libfido2 - Copyright Yubico AB under BSD-2-Clause License
libsodium - Copyright Frank Denis under ISC License
LiteDB - Copyright Mauricio David under MIT License
NSec.Cryptography - Copyright Klaus Hartke under MIT License
Otp.NET - Copyright Kyle Spearrin under MIT License
Serilog - Copyright Serilog Contributors under Apache License 2.0
Simple Icons - Copyright under CC0 1.0 Universal
SkiaSharp - Copyright Microsoft Corporation under MIT License
Tmds.DBus - Copyright Tom Deseyn under MIT License
ZXing.Net - Copyright Michael Jahn under Apache 2.0 License",
                maxWidth: 600
            );
        }

        private async void Reset_Button_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (Auth.IsLoginEnabled())
                {
                    byte[]? password = await Dialogs.PasswordDialog();
                    if (password == null)
                    {
                        return;
                    }
                    if (!Auth.CheckPassword(password))
                    {
                        throw new Exception(I18n.GetString("passdialog.incorrect"));
                    }
                }

                var confirmResult = await Dialogs.MessageBox(
                    I18n.GetString("settings.reset.confirm.title"),
                    I18n.GetString("settings.reset.confirm.content"),
                    closeButtonText: I18n.GetString("dialog.cancel"),
                    primaryButtonText: I18n.GetString("settings.reset.confirm.btn"),
                    maxWidth: 500,
                    primaryIsDanger: true
                );

                if (confirmResult == DialogResult.Primary)
                {
                    Reset.DeleteEverything();
                    if (!InstallationContext.IsPortable())
                    {
                        await Autostart.Disable();
                    }
                    App.Instance.Quit();
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Error resetting app: {0} {1}", ex.Message, ex.StackTrace);
                await Dialogs.MessageBox(I18n.GetString("error"), ex.Message, maxWidth: 500);
            }
        }

        private async void WebAuthn_Button_Click(object? sender, RoutedEventArgs e)
        {
            if (!Fido2.IsSupported())
            {
                await Dialogs.MessageBox(
                    I18n.GetString("error"),
                    I18n.GetString("settings.webauthn.notsupported")
                );
                return;
            }
            mainWindow.Navigate(typeof(WebAuthnPage), true);
        }

        private static void FixUniformGridSpacing(UniformGrid grid, bool multipleCols)
        {
            int i = 0;
            foreach (Control card in grid.Children)
            {
                if (!card.IsVisible)
                {
                    continue;
                }
                card.Margin = multipleCols && i % 2 != 0
                    ? new Thickness(15, 0, 0, 15)
                    : new Thickness(0, 0, 0, 15);
                ++i;
            }
        }

        private void OnWindowSizeChanged(object? sender, (double width, double height) size)
        {
            bool onlyRows = size.width < 800;
            int columns = onlyRows ? 1 : 2;

            PersonalisationSettingsGrid.Columns = columns;
            SecuritySettingsGrid.Columns = columns;
            FixUniformGridSpacing(PersonalisationSettingsGrid, !onlyRows);
            FixUniformGridSpacing(SecuritySettingsGrid, !onlyRows);

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
                AppVersionText.Margin = new Thickness(0);
            }
        }
    }
}
