using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using OtpNet;
using TOTPTokenGuard.Core;
using TOTPTokenGuard.Core.Icons;
using TOTPTokenGuard.Core.Models;
using TOTPTokenGuard.Core.Security;
using TOTPTokenGuard.Views.UIComponents;
using Wpf.Ui.Controls;

namespace TOTPTokenGuard.Views.Pages.Add
{
    /// <summary>
    /// Interaktionslogik für TokenDetails.xaml
    /// </summary>
    public partial class TokenDetails : Page
    {
        private IconManager.TotpIcon? selectedIcon;
        private IconManager.TotpIcon defaultIcon;

        public TokenDetails()
        {
            InitializeComponent();
            defaultIcon = IconManager.GetIcon(
                "default",
                IconManager.IconColor.Colored,
                IconManager.IconType.Default
            );
            IconSvgView.SvgSource = defaultIcon.Svg;
            Issuer.OriginalItemsSource = IconManager.GetIconNames();

            foreach (string algorithm in new[] { "SHA1", "SHA256", "SHA512" })
            {
                AlgorithmComboBox.Items.Add(
                    new ComboBoxItem { Content = algorithm, Tag = algorithm }
                );
            }
            AlgorithmComboBox.SelectedItem = AlgorithmComboBox
                .Items.OfType<ComboBoxItem>()
                .FirstOrDefault(x => (string)x.Tag == "SHA1");

            Issuer.SuggestionChosen += AutoSuggestBoxOnSuggestionChosen;
            Issuer.TextChanged += AutoSuggestBoxOnTextChanged;
        }

        private void AutoSuggestBoxOnSuggestionChosen(
            AutoSuggestBox sender,
            AutoSuggestBoxSuggestionChosenEventArgs args
        )
        {
            if (sender.IsSuggestionListOpen)
                return;

            if (args.SelectedItem is not string selectedSuggestBoxItem)
                return;

            selectedIcon = IconManager.GetIcon(
                selectedSuggestBoxItem,
                IconManager.IconColor.Colored,
                IconManager.IconType.Any
            );
            IconSvgView.SvgSource = selectedIcon.Svg;
            ImageLicense.Text = IconManager.GetLicense(selectedIcon);
            NoIconText.Visibility = Visibility.Collapsed;
        }

        private void AutoSuggestBoxOnTextChanged(
            AutoSuggestBox sender,
            AutoSuggestBoxTextChangedEventArgs args
        )
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                IconSvgView.SvgSource = defaultIcon.Svg;
                ImageLicense.Text = string.Empty;
                NoIconText.Visibility = Visibility.Visible;
            }
        }

        private void FormatButton_Click(object sender, RoutedEventArgs e)
        {
            TextFormattingDialog.Show();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            InfoBar.IsOpen = false;

            if (string.IsNullOrWhiteSpace(Issuer.Text))
            {
                ShowEror(I18n.GetString("td.noissuer"));
                return;
            }
            // Validate secret
            if (string.IsNullOrWhiteSpace(Secret.Password))
            {
                ShowEror(I18n.GetString("td.nosecret"));
                return;
            }
            try
            {
                Base32Encoding.ToBytes(Secret.Password);
            }
            catch
            {
                ShowEror(I18n.GetString("td.invalidsecret"));
                return;
            }
            // Validate digits
            if (!int.TryParse(DigitsBox.Text, out int digits) || digits < 4 || digits > 10)
            {
                ShowEror(I18n.GetString("td.invaliddigits"));
                return;
            }
            // Validate period
            if (!int.TryParse(PeriodBox.Text, out int period) || period < 1 || period > 3600)
            {
                ShowEror(I18n.GetString("td.invalidperiod"));
                return;
            }

            SaveButton.IsEnabled = false;

            try
            {
                EncryptionHelper encryptionHelper = Auth.GetMainEncryptionHelper();
                DBTOTPToken dbToken =
                    new()
                    {
                        Id = TokenManager.GetNextId(),
                        Issuer = Issuer.Text,
                        EncryptedSecret = encryptionHelper.EncryptString(Secret.Password),
                    };

                if (digits != 6)
                {
                    dbToken.Digits = digits;
                }
                TOTPAlgorithm algorithm = (TOTPAlgorithm)AlgorithmComboBox.SelectedIndex;
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

                if (!string.IsNullOrWhiteSpace(Username.Text))
                {
                    dbToken.Username = Username.Text;
                }

                string notes = new TextRange(
                    Notes.Document.ContentStart,
                    Notes.Document.ContentEnd
                ).Text;

                if (!string.IsNullOrWhiteSpace(notes) && !notes.Equals("\r\n"))
                {
                    dbToken.EncryptedNotes = encryptionHelper.EncryptString(notes);
                }

                TokenManager.AddToken(dbToken);
            }
            catch (Exception ex)
            {
                ShowEror(ex.Message);
                SaveButton.IsEnabled = true;
            }
        }

        private void ShowEror(string message)
        {
            InfoBar.Title = I18n.GetString("error");
            InfoBar.Message = message;
            InfoBar.Severity = InfoBarSeverity.Error;
            InfoBar.IsOpen = true;
        }
    }
}
