using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using FluentAvalonia.UI.Controls;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Linux.Core;
using Guard.Linux.Views.Controls;
using Guard.WPF.Core;
using Guard.WPF.Core.Icons;
using Guard.WPF.Core.Import;

namespace Guard.Linux.Views.Pages.Add
{
    public partial class TokenSettings : UserControl
    {
        private const string IconFilter = "Icon (*.jpg, *.jpeg, *.png, *.svg) | *.jpg; *.jpeg; *.png; *.svg";

        private TotpIcon? selectedIcon;
        private readonly TotpIcon defaultIcon;
        private readonly MainWindow mainWindow;
        private readonly string action;
        private readonly TOTPTokenHelper? existingToken;
        private readonly Dictionary<string, string> iconNames;

        // Notes of an existing token, kept unchanged if the user does not edit them
        private byte[]? originalNotes;
        private string originalNotesText = "";

        public TokenSettings()
        {
            InitializeComponent();
            mainWindow = App.MainWindow!;

            action =
                NavigationContextManager.CurrentContext["action"] as string
                ?? throw new Exception("No action specified for TokenSettings");

            if (action != "add" && action != "edit")
            {
                throw new Exception("Invalid action specified for TokenSettings");
            }

            if (action == "edit")
            {
                int tokenID = (int)NavigationContextManager.CurrentContext["tokenID"];
                existingToken = TokenManager.GetTokenById(tokenID);
            }

            NavigationContextManager.ClearContext();

            defaultIcon = IconManager.GetIcon("default", IconType.Default);
            IconSvgView.Source = defaultIcon.Svg;
            string[] names = IconManager.GetIconNames();
            Issuer.ItemsSource = names;
            iconNames = names
                .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            if (existingToken != null)
            {
                LoadExistingToken(existingToken);
            }

            Issuer.TextChanged += (_, _) => OnIssuerChanged();

            Loaded += (_, _) =>
            {
                mainWindow.SetPageTitle(
                    I18n.GetString(action == "edit" ? "i.page.tokensettings.edit" : "i.page.tokensettings.add")
                );
                WPF.Core.EventManager.WindowSizeChanged += OnWindowSizeChanged;
                OnWindowSizeChanged(this, (mainWindow.Bounds.Width, mainWindow.Bounds.Height));
                if (existingToken == null)
                {
                    Issuer.Focus();
                }
            };

            Unloaded += (_, _) => WPF.Core.EventManager.WindowSizeChanged -= OnWindowSizeChanged;
        }

        private void LoadExistingToken(TOTPTokenHelper token)
        {
            if (string.IsNullOrEmpty(token.dBToken.Issuer))
            {
                token.dBToken.Issuer = "???";
            }
            EncryptionHelper encryptionHelper = Auth.GetMainEncryptionHelper();
            Issuer.Text = token.dBToken.Issuer;
            Secret.Text = encryptionHelper.DecryptBytesToString(token.dBToken.EncryptedSecret);

            if (token.dBToken.Icon != null)
            {
                selectedIcon = IconManager.GetIcon(token.dBToken.Icon, token.dBToken.IconType ?? IconType.Any);
                ShowSelectedIcon();
            }
            if (token.dBToken.EncryptedUsername != null)
            {
                Username.Text = encryptionHelper.DecryptBytesToString(token.dBToken.EncryptedUsername);
            }
            if (token.dBToken.EncryptedNotes != null)
            {
                try
                {
                    originalNotes = encryptionHelper.DecryptBytes(token.dBToken.EncryptedNotes);
                    (originalNotesText, bool hasFormatting) = NotesFormat.ToPlainText(originalNotes);
                    Notes.Text = originalNotesText;
                    NotesFormattedHint.IsVisible = hasFormatting;
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
            if (token.dBToken.Algorithm != null)
            {
                AlgorithmComboBox.SelectedIndex = (int)token.dBToken.Algorithm;
            }
            if (token.dBToken.Digits != null)
            {
                DigitsBox.Value = token.dBToken.Digits.Value;
            }
            if (token.dBToken.Period != null)
            {
                PeriodBox.Value = token.dBToken.Period.Value;
            }
        }

        private void OnIssuerChanged()
        {
            if (selectedIcon != null && selectedIcon.Type == IconType.Custom)
            {
                return;
            }

            if (iconNames.TryGetValue(Issuer.Text?.Trim() ?? "", out string? iconName))
            {
                if (selectedIcon?.Name != iconName)
                {
                    selectedIcon = IconManager.GetIcon(iconName, IconType.Any);
                    ShowSelectedIcon();
                }
                return;
            }

            selectedIcon = null;
            ImageIconView.IsVisible = false;
            IconSvgView.IsVisible = true;
            IconSvgView.Source = defaultIcon.Svg;
            ImageLicense.Text = string.Empty;
            NoIconText.IsVisible = true;
        }

        private void ShowSelectedIcon()
        {
            if (selectedIcon == null)
            {
                return;
            }
            if (!IconView.Show(selectedIcon, IconSvgView, ImageIconView))
            {
                return;
            }
            ImageLicense.Text = selectedIcon.Type == IconType.Custom ? "" : IconManager.GetLicense(selectedIcon);
            NoIconText.IsVisible = false;
        }

        private void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;

            string issuer = Issuer.Text?.Trim() ?? "";
            string secret = Secret.Text ?? "";

            if (string.IsNullOrWhiteSpace(issuer))
            {
                ShowError(I18n.GetString("td.noissuer"));
                return;
            }
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 2)
            {
                ShowError(I18n.GetString("td.nosecret"));
                return;
            }

            string normalizedSecret = OTPUriParser.NormalizeSecret(secret);
            if (!OTPUriParser.IsValidSecret(normalizedSecret))
            {
                ShowError(I18n.GetString("td.invalidsecret"));
                return;
            }

            if (double.IsNaN(DigitsBox.Value) || DigitsBox.Value < 4 || DigitsBox.Value > 9)
            {
                ShowError(I18n.GetString("td.invaliddigits"));
                return;
            }
            int digits = (int)DigitsBox.Value;

            if (double.IsNaN(PeriodBox.Value) || PeriodBox.Value < 1 || PeriodBox.Value > 3600)
            {
                ShowError(I18n.GetString("td.invalidperiod"));
                return;
            }
            int period = (int)PeriodBox.Value;

            SaveButton.IsEnabled = false;

            try
            {
                EncryptionHelper encryptionHelper = Auth.GetMainEncryptionHelper();
                DBTOTPToken dbToken = new()
                {
                    Id = existingToken != null ? existingToken.dBToken.Id : TokenManager.GetNextId(),
                    Issuer = issuer,
                    EncryptedSecret = encryptionHelper.EncryptStringToBytes(normalizedSecret),
                    CreationTime = DateTime.Now,
                };

                if (digits != 6)
                {
                    dbToken.Digits = digits;
                }
                TOTPAlgorithm algorithm = (TOTPAlgorithm)Math.Max(0, AlgorithmComboBox.SelectedIndex);
                if (algorithm != TOTPAlgorithm.SHA1)
                {
                    dbToken.Algorithm = algorithm;
                }
                if (period != 30)
                {
                    dbToken.Period = period;
                }

                if (selectedIcon != null)
                {
                    dbToken.Icon = selectedIcon.Name;
                    dbToken.IconType = selectedIcon.Type;
                }

                dbToken.EncryptedUsername = !string.IsNullOrWhiteSpace(Username.Text)
                    ? encryptionHelper.EncryptStringToBytes(Username.Text)
                    : null;

                string notesText = Notes.Text ?? "";
                if (originalNotes != null && notesText == originalNotesText)
                {
                    dbToken.EncryptedNotes = encryptionHelper.EncryptBytes(originalNotes);
                }
                else if (!string.IsNullOrWhiteSpace(notesText))
                {
                    dbToken.EncryptedNotes = encryptionHelper.EncryptBytes(NotesFormat.FromPlainText(notesText));
                }
                else
                {
                    dbToken.EncryptedNotes = null;
                }

                if (existingToken != null)
                {
                    TokenManager.DeleteTokenById(dbToken.Id);
                    dbToken.CreationTime = existingToken.dBToken.CreationTime;
                    dbToken.UpdatedTime = DateTime.Now;
                    TokenManager.AddToken(dbToken);

                    NavigationContextManager.CurrentContext["tokenID"] = dbToken.Id;
                    NavigationContextManager.CurrentContext["type"] = "edited";
                    mainWindow.Navigate(typeof(TokenSuccessPage));
                    return;
                }

                dbToken.UpdatedTime = DateTime.Now;

                if (!TokenManager.AddToken(dbToken))
                {
                    throw new Exception(I18n.GetString("import.duplicate"));
                }

                NavigationContextManager.CurrentContext["tokenID"] = dbToken.Id;
                NavigationContextManager.CurrentContext["type"] = "added";
                mainWindow.Navigate(typeof(TokenSuccessPage));
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                SaveButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            InfoBar.Title = I18n.GetString("error");
            InfoBar.Message = message;
            InfoBar.Severity = InfoBarSeverity.Error;
            InfoBar.IsOpen = true;
        }

        private void ExpertSettings_Button_Click(object? sender, RoutedEventArgs e)
        {
            ExpertButton.IsVisible = false;
            ExpertWarningBar.IsOpen = false;
            ExpertWarningBar.IsVisible = false;
            AlgorithmComboBox.IsEnabled = true;
            DigitsBox.IsEnabled = true;
            PeriodBox.IsEnabled = true;
        }

        private async void CustomIcon_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                string? path = await FileDialogs.OpenFile(
                    IconFilter,
                    I18n.GetString("i.td.customicon.dialog.title")
                );
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                string name = await IconManager.AddCustomIcon(path);

                if (selectedIcon != null && selectedIcon.Type == IconType.Custom)
                {
                    IconManager.RemoveCustomIcon(selectedIcon.Name);
                }

                selectedIcon = IconManager.GetIcon(name, IconType.Custom);
                ShowSelectedIcon();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void OnWindowSizeChanged(object? sender, (double width, double height) size)
        {
            if (size.width < 800)
            {
                LeftStackPanel.Margin = new Thickness(0);
                CenterStackPanel.Margin = new Thickness(0, 20, 0, 0);
                RightStackPanel.Margin = new Thickness(0, 20, 0, 0);
            }
            else
            {
                LeftStackPanel.Margin = new Thickness(0, 0, 25, 0);
                CenterStackPanel.Margin = new Thickness(50, 0, 50, 0);
                RightStackPanel.Margin = new Thickness(50, 0, 0, 0);
            }

            if (size.width < 600)
            {
                LeftStackPanel.HorizontalAlignment = HorizontalAlignment.Center;
                LeftStackPanel.Width = Math.Max(150, size.width - 150);
            }
            else
            {
                LeftStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
                LeftStackPanel.Width = 200;
            }
        }
    }
}
