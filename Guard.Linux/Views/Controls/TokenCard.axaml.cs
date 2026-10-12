using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Guard.Core;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Core.Storage;
using Guard.Linux.Core;
using Guard.Linux.Views.Pages;
using Guard.Linux.Views.Pages.Add;
using Guard.WPF.Core;
using Guard.WPF.Core.Export;
using Guard.WPF.Core.Icons;

namespace Guard.Linux.Views.Controls
{
    public partial class TokenCard : UserControl
    {
        private static readonly IBrush tokenHiddenDark = new SolidColorBrush(Color.FromRgb(204, 204, 204));
        private static readonly IBrush tokenHiddenLight = new SolidColorBrush(Color.FromRgb(68, 68, 68));

        private readonly TOTPTokenHelper token;
        private readonly MainWindow mainWindow;
        internal readonly string SearchString;
        private TotpIcon? icon;
        private long lastCounter = -1;
        private bool notesLoaded;

        public TokenCard()
        {
            throw new NotSupportedException();
        }

        internal TokenCard(TOTPTokenHelper token)
        {
            InitializeComponent();
            this.token = token;
            mainWindow = App.MainWindow!;

            if (string.IsNullOrEmpty(token.dBToken.Issuer))
            {
                token.dBToken.Issuer = "???";
            }

            Issuer.Text = token.dBToken.Issuer;
            if (token.dBToken.Issuer.Length > 18)
            {
                ToolTip.SetTip(Issuer, token.dBToken.Issuer);
            }
            if (token.Username != null)
            {
                Username.Text = token.Username;
                if (token.Username.Length > 22)
                {
                    ToolTip.SetTip(Username, token.Username);
                }
            }
            else
            {
                Username.IsVisible = false;
            }

            SetIcon();
            Update();

            Loaded += (_, _) => WPF.Core.EventManager.AppThemeChanged += OnAppThemeChanged;
            Unloaded += (_, _) => WPF.Core.EventManager.AppThemeChanged -= OnAppThemeChanged;

            SearchString = $"{token.dBToken.Issuer.ToLower()} {token.Username?.ToLower()}";

            Card.PointerReleased += (_, e) =>
            {
                if (e.InitialPressMouseButton != MouseButton.Left)
                {
                    return;
                }
                if (SettingsManager.Settings.HideToken == HideTokenSetting.ShowAfterClick)
                {
                    UpdateTokenText(true);
                }
                CopyToken();
            };

            KeyDown += (_, e) =>
            {
                switch (e.Key)
                {
                    case Key.Enter or Key.C or Key.Space:
                        CopyToken();
                        e.Handled = true;
                        break;
                    case Key.Q:
                        ShowQrCode();
                        e.Handled = true;
                        break;
                    case Key.E:
                        Edit();
                        e.Handled = true;
                        break;
                    case Key.Delete:
                        Delete();
                        e.Handled = true;
                        break;
                }
            };

            if (token.dBToken.EncryptedNotes != null)
            {
                PointerEntered += (_, _) => AddNotesTooltip();
            }
        }

        internal void Update()
        {
            long periodMs = (token.dBToken.Period ?? 30) * 1000L;
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long remainingMs = periodMs - (nowMs % periodMs);
            long counter = nowMs / periodMs;

            if (counter != lastCounter)
            {
                lastCounter = counter;
                TimeProgressRing.ClearValue(ForegroundProperty);
                UpdateTokenText();
                var transitions = TimeProgressRing.Transitions;
                TimeProgressRing.Transitions = null;
                TimeProgressRing.Value = remainingMs * 100.0 / periodMs;
                TimeProgressRing.Transitions = transitions;
            }

            // Animate to the value of the next tick
            TimeProgressRing.Value = Math.Max(0, (remainingMs - 1000) * 100.0 / periodMs);
            if (remainingMs < 6000)
            {
                TimeProgressRing.Foreground = Brushes.Red;
            }
        }

        private void UpdateTokenText(bool showIfHidden = false)
        {
            bool isDarkMode = IconColor.IsDarkMode();
            if (showIfHidden)
            {
                TokenTextBlock.ClearValue(TextBlock.ForegroundProperty);
            }
            else if (SettingsManager.Settings.HideToken != HideTokenSetting.Never)
            {
                TokenTextBlock.Text = "●●● ●●●";
                TokenTextBlock.Foreground = isDarkMode ? tokenHiddenDark : tokenHiddenLight;
                return;
            }

            string tokenStr = token.GenerateToken();
            if (tokenStr.Length == 6)
            {
                tokenStr = tokenStr[..3] + " " + tokenStr[3..];
            }
            else if (tokenStr.Length == 8)
            {
                tokenStr = tokenStr[..4] + " " + tokenStr[4..];
            }

            TokenTextBlock.Text = tokenStr;
        }

        internal void SetIcon()
        {
            icon = token.dBToken.Icon != null && token.dBToken.IconType != null
                ? IconManager.GetIcon(token.dBToken.Icon, token.dBToken.IconType ?? IconType.Any)
                : IconManager.GetIcon("default", IconType.Default);

            if (!IconView.Show(icon, SvgIconView, ImageIconView))
            {
                Log.Logger.Warning("Custom icon could not be loaded: {Path}", icon.Path);
                icon = IconManager.GetIcon("default", IconType.Default);
                IconView.Show(icon, SvgIconView, ImageIconView);
            }
        }

        private void OnAppThemeChanged(object? sender, WPF.Core.EventManager.AppThemeChangedEventArgs e)
        {
            if (icon != null && icon.Type == IconType.SimpleIcons)
            {
                SetIcon();
            }
            if (SettingsManager.Settings.HideToken != HideTokenSetting.Never)
            {
                UpdateTokenText();
            }
        }

        private async void CopyToken()
        {
            try
            {
                TimeProgressRing.IsVisible = false;
                SvgIconRingView.IsVisible = true;
                string copiedToken = token.GenerateToken();
                await ClipboardHelper.SetSecretText(copiedToken);

                var clearSetting =
                    RegistrySettings.GetForcedClearClipboard()
                    ?? SettingsManager.Settings.ClearClipboard;
                if (clearSetting != ClearClipboardSetting.Disabled)
                {
                    ClipboardClearService.Schedule(
                        copiedToken,
                        ClipboardClearService.GetClearClipboardMs(clearSetting)
                    );
                }

                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Failed to copy token: {0}", ex.Message);
            }
            finally
            {
                SvgIconRingView.IsVisible = false;
                TimeProgressRing.IsVisible = true;
            }
        }

        private void MenuItem_Copy_Click(object? sender, RoutedEventArgs e) => CopyToken();

        private void MenuItem_Edit_Click(object? sender, RoutedEventArgs e) => Edit();

        private void MenuItem_Delete_Click(object? sender, RoutedEventArgs e) => Delete();

        private void MenuItem_Qr_Click(object? sender, RoutedEventArgs e) => ShowQrCode();

        private void Edit()
        {
            NavigationContextManager.CurrentContext["tokenID"] = token.dBToken.Id;
            NavigationContextManager.CurrentContext["action"] = "edit";
            bool showBackButton = mainWindow.GetActivePage() == nameof(Home);
            mainWindow.Navigate(typeof(TokenSettings), showBackButton);
        }

        private async void Delete()
        {
            var result = await Dialogs.MessageBox(
                I18n.GetString("tokencard.delete.modal.title"),
                I18n.GetString("tokencard.delete.modal.content").Replace("@Name", token.dBToken.Issuer),
                primaryButtonText: I18n.GetString("tokencard.delete.modal.yes"),
                primaryIsDanger: true
            );
            if (result != DialogResult.Primary)
            {
                return;
            }
            TokenManager.DeleteTokenById(token.dBToken.Id);
            if (token.dBToken.Icon != null && token.dBToken.IconType == IconType.Custom)
            {
                IconManager.RemoveCustomIcon(token.dBToken.Icon);
            }
            if (mainWindow.GetActivePage() != nameof(Home))
            {
                mainWindow.Navigate(typeof(Home));
                return;
            }
            WPF.Core.EventManager.EmitTokenDeleted(token.dBToken.Id);
        }

        internal string GetIssuer() => token.dBToken.Issuer;

        internal DateTime GetCreationTime() => token.dBToken.CreationTime;

        internal int GetTokenId() => token.dBToken.Id;

        private async void ShowQrCode()
        {
            try
            {
                string uri = OTPUriCreator.GetUri(token);
                byte[] png = QRCode.GenerateQRCodePng(uri, 380);
                using Bitmap qrImage = new(new MemoryStream(png));
                var result = await Dialogs.QRDialog(qrImage, 380);
                if (result == DialogResult.Primary)
                {
                    var file = await mainWindow.StorageProvider.SaveFilePickerAsync(
                        new FilePickerSaveOptions
                        {
                            SuggestedFileName = $"{token.dBToken.Issuer}.png",
                            DefaultExtension = "png",
                            FileTypeChoices = [new FilePickerFileType("PNG") { Patterns = ["*.png"] }],
                        }
                    );
                    if (file != null)
                    {
                        await using Stream stream = await file.OpenWriteAsync();
                        await stream.WriteAsync(png);
                    }
                }
                else if (result == DialogResult.Secondary)
                {
                    await ClipboardHelper.SetPng(qrImage);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error("QR code action failed: {0}", ex.Message);
                await Dialogs.Error(ex.Message);
            }
        }

        private void AddNotesTooltip()
        {
            if (notesLoaded || token.dBToken.EncryptedNotes == null)
            {
                return;
            }
            notesLoaded = true;
            try
            {
                byte[] notesBytes = Auth.GetMainEncryptionHelper()
                    .DecryptBytes(token.dBToken.EncryptedNotes);
                (string text, _) = NotesFormat.ToPlainText(notesBytes);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return;
                }
                ToolTip.SetTip(
                    this,
                    new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 400 }
                );
                ToolTip.SetShowDelay(this, 400);
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Error loading notes for token: {Message}", ex);
            }
        }
    }
}
