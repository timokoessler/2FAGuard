using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Core.Models;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace TOTPTokenGuard.Views.Pages
{
    /// <summary>
    /// Interaktionslogik für Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        private readonly MainWindow mainWindow;
        public Settings()
        {
            InitializeComponent();

            mainWindow = (MainWindow)Application.Current.MainWindow;

            AppInfoTextBlock.Text =
                $"Copyright © {DateTime.Now.Year} Timo Kössler and Open Source Contributors\n";
            AppInfoTextBlock.Text += $"Version {Utils.GetVersionString()}";
            if (SettingsManager.Settings.Theme == ThemeSetting.System)
            {
                SystemThemeRadioButton.IsChecked = true;
            }
            else if (SettingsManager.Settings.Theme == ThemeSetting.Dark)
            {
                DarkThemeRadioButton.IsChecked = true;
            }
            else
            {
                LightThemeRadioButton.IsChecked = true;
            }

            SystemThemeRadioButton.Checked += OnSystemThemeRadioButtonChecked;
            LightThemeRadioButton.Checked += OnLightThemeRadioButtonChecked;
            DarkThemeRadioButton.Checked += OnDarkThemeRadioButtonChecked;

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
        }

        private void SetSelectedLanguage(LanguageSetting lang)
        {
            LanguagesComboBox.SelectedItem = LanguagesComboBox
                .Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x => (string)x.Tag == lang.ToString().ToLower());
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

        private void OnSystemThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            mainWindow.ApplyTheme(ThemeSetting.System);
            SettingsManager.Settings.Theme = ThemeSetting.System;
            _ = SettingsManager.Save();
        }

        private void OnLightThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            mainWindow.ApplyTheme(ThemeSetting.Light);
            SettingsManager.Settings.Theme = ThemeSetting.Light;
            _ = SettingsManager.Save();
        }

        private void OnDarkThemeRadioButtonChecked(object sender, RoutedEventArgs e)
        {
            mainWindow.ApplyTheme(ThemeSetting.Dark);
            SettingsManager.Settings.Theme = ThemeSetting.Dark;
            _ = SettingsManager.Save();
        }
    }
}
