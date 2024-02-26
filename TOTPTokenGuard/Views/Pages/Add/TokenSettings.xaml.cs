using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
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
    public partial class TokenSettings : Page
    {
        private IconManager.TotpIcon? selectedIcon;
        private readonly IconManager.TotpIcon defaultIcon;
        private readonly MainWindow mainWindow;
        private readonly string action;
        private readonly TOTPTokenHelper? existingToken;

        public TokenSettings()
        {
            InitializeComponent();
            mainWindow = (MainWindow)Application.Current.MainWindow;

            action =
                NavigationContextManager.CurrentContext["action"] as string
                ?? throw new System.Exception("No action specified for TokenSettings");

            if (action != "add" && action != "edit")
            {
                throw new System.Exception("Invalid action specified for TokenSettings");
            }

            if (action.Equals("edit"))
            {
                int tokenID = (int)NavigationContextManager.CurrentContext["tokenID"];
                existingToken = TokenManager.GetTokenById(tokenID);
            }
            NavigationContextManager.ClearContext();

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

            // Set values if action is edit
            if (existingToken != null)
            {
                EncryptionHelper encryptionHelper = Auth.GetMainEncryptionHelper();
                Issuer.Text = existingToken.dBToken.Issuer;
                Secret.Password = encryptionHelper.DecryptString(
                    existingToken.dBToken.EncryptedSecret
                );
                if (existingToken.dBToken.Icon != null)
                {
                    selectedIcon = IconManager.GetIcon(
                        existingToken.dBToken.Icon,
                        IconManager.IconColor.Colored,
                        existingToken.dBToken.IconType ?? IconManager.IconType.Any
                    );
                    IconSvgView.SvgSource = selectedIcon.Svg;
                    ImageLicense.Text = IconManager.GetLicense(selectedIcon);
                    NoIconText.Visibility = Visibility.Collapsed;
                }
                if (existingToken.dBToken.Username != null)
                {
                    Username.Text = existingToken.dBToken.Username;
                }
                if (existingToken.dBToken.EncryptedNotes != null)
                {
                    try
                    {
                        MemoryStream notesStream =
                            new(
                                Encoding.Default.GetBytes(
                                    encryptionHelper.DecryptString(
                                        existingToken.dBToken.EncryptedNotes
                                    )
                                )
                            );
                        TextRange notesRange =
                            new(Notes.Document.ContentStart, Notes.Document.ContentEnd);
                        notesRange.Load(notesStream, DataFormats.Xaml);
                        notesStream.Close();
                    }
                    catch (Exception ex)
                    {
                        ShowEror(ex.Message);
                    }
                }
                if (existingToken.dBToken.Algorithm != null)
                {
                    AlgorithmComboBox.SelectedItem = AlgorithmComboBox
                        .Items.OfType<ComboBoxItem>()
                        .FirstOrDefault(x =>
                            (string)x.Tag == existingToken.dBToken.Algorithm.ToString()
                        );
                }
                if (existingToken.dBToken.Digits != null)
                {
                    DigitsBox.Text = existingToken.dBToken.Digits.ToString();
                }
                if (existingToken.dBToken.Period != null)
                {
                    PeriodBox.Text = existingToken.dBToken.Period.ToString();
                }

                Loaded += (sender, e) =>
                {
                    Issuer.Text = existingToken.dBToken.Issuer;
                };
            }

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
            if (!int.TryParse(DigitsBox.Text, out int digits) || digits < 4 || digits > 9)
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

            // If action is not edit, check for duplicate

            SaveButton.IsEnabled = false;

            try
            {
                EncryptionHelper encryptionHelper = Auth.GetMainEncryptionHelper();
                DBTOTPToken dbToken =
                    new()
                    {
                        Id =
                            existingToken != null
                                ? existingToken.dBToken.Id
                                : TokenManager.GetNextId(),
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

                TextRange notesTextRange =
                    new(Notes.Document.ContentStart, Notes.Document.ContentEnd);

                if (
                    !string.IsNullOrWhiteSpace(notesTextRange.Text)
                    && !notesTextRange.Text.Equals("\r\n")
                )
                {
                    MemoryStream notesStream = new();
                    notesTextRange.Save(notesStream, DataFormats.Xaml);
                    string notesXamlString = Encoding.Default.GetString(notesStream.ToArray());
                    notesStream.Close();
                    dbToken.EncryptedNotes = encryptionHelper.EncryptString(notesXamlString);
                }

                if (existingToken != null)
                {
                    TokenManager.DeleteTokenById(dbToken.Id);
                    TokenManager.AddToken(dbToken);
                    mainWindow.GetStatsClient()?.TrackEvent("TokenEdited");

                    NavigationContextManager.CurrentContext["tokenID"] = dbToken.Id;
                    NavigationContextManager.CurrentContext["type"] = "edited";
                    mainWindow.Navigate(typeof(TokenSuccessPage));
                    return;
                }

                TokenManager.AddToken(dbToken);

                mainWindow.GetStatsClient()?.TrackEvent("TokenAddedManually");

                NavigationContextManager.CurrentContext["tokenID"] = dbToken.Id;
                NavigationContextManager.CurrentContext["type"] = "added";
                mainWindow.Navigate(typeof(TokenSuccessPage));
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
