using Avalonia.Controls;
using Avalonia.Interactivity;
using Guard.Core;
using Guard.Linux.Core;
using Guard.Linux.Core.Import;
using Guard.WPF.Core;
using Guard.WPF.Core.Import.Importer;

namespace Guard.Linux.Views.Pages.Add
{
    public partial class AddOverview : UserControl
    {
        private readonly MainWindow mainWindow;
        private bool importRunning;

        public AddOverview()
        {
            InitializeComponent();
            mainWindow = App.MainWindow!;
        }

        private void Manual_Click(object? sender, RoutedEventArgs e)
        {
            NavigationContextManager.CurrentContext["action"] = "add";
            mainWindow.Navigate(typeof(TokenSettings), true);
        }

        private void Qr_Import_Click(object? sender, RoutedEventArgs e) => Import(new QRFileImporter());

        private void Clipboard_Click(object? sender, RoutedEventArgs e) => Import(new ClipboardImporter());

        private void UriList_Click(object? sender, RoutedEventArgs e) => Import(new UriListImporter());

        private void Bitwarden_Click(object? sender, RoutedEventArgs e) => Import(new BitwardenImporter());

        private void AuthenticatorPro_Click(object? sender, RoutedEventArgs e) =>
            Import(new AuthenticatorProImporter());

        private void Backup_Click(object? sender, RoutedEventArgs e) => Import(new BackupImporter());

        private void TwoFas_Click(object? sender, RoutedEventArgs e) => Import(new TwoFasImporter());

        private void Aegis_Click(object? sender, RoutedEventArgs e) =>
            Import(new AegisAuthenticatorImporter());

        private void QR_ScreenCapture_Click(object? sender, RoutedEventArgs e) =>
            Import(new QRScreenCaptureImporter());

        private void ProtonAuthenticator_Click(object? sender, RoutedEventArgs e) =>
            Import(new ProtonAuthenticatorImporter());

        private async void GAuthenticator_Click(object? sender, RoutedEventArgs e)
        {
            await Dialogs.MessageBox(
                I18n.GetString("import.gauthenticator"),
                I18n.GetString("import.gauthenticator.msgbox.content").Replace("@n", "\n"),
                maxWidth: 500
            );
        }

        private async void Import(IImporter importer)
        {
            if (importRunning)
            {
                return;
            }
            importRunning = true;
            try
            {
                var result = await RunImporter(importer);
                if (result is (int total, int duplicate, int tokenID))
                {
                    ShowImportResult(total, duplicate, tokenID);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(
                    "Import of {importer} failed: {message} {stacktrace}",
                    importer.Name,
                    ex.Message,
                    ex.StackTrace
                );
                string content = ex.Message.Contains("Decryption failed (null)")
                    ? I18n.GetString("import.password.invalid")
                    : I18n.GetString("import.failed.content") + " " + ex.Message;

                await Dialogs.MessageBox(I18n.GetString("import.failed.title"), content);
            }
            finally
            {
                importRunning = false;
            }
        }

        /// <summary>
        /// Returns null if the user canceled the import.
        /// </summary>
        private static async Task<(int total, int duplicate, int tokenID)?> RunImporter(IImporter importer)
        {
            if (importer.Type == IImporter.ImportType.File)
            {
                return await RunFileImporter(importer);
            }
            if (importer is ClipboardImporter clipboardImporter)
            {
                await clipboardImporter.LoadClipboard();
                return importer.Parse(null, null);
            }
            if (importer is QRScreenCaptureImporter screenImporter)
            {
                try
                {
                    await screenImporter.Capture();
                }
                catch (ScreenCapture.ScreenshotDeniedException)
                {
                    await Dialogs.MessageBox(
                        I18n.GetString("import.failed.title"),
                        I18n.GetString("linux.screenshot.denied")
                    );
                    return null;
                }
                return importer.Parse(null, null);
            }
            throw new Exception("Invalid Importer Type");
        }

        private static async Task<(int total, int duplicate, int tokenID)?> RunFileImporter(IImporter importer)
        {
            string? path = await FileDialogs.OpenFile(importer.SupportedFileExtensions);
            if (path == null)
            {
                return null;
            }
            byte[]? password = null;
            if (importer.RequiresPassword(path))
            {
                password = await Dialogs.PasswordDialog(I18n.GetString("import.password"));
                if (password == null)
                {
                    return null;
                }
                if (password.Length == 0)
                {
                    throw new Exception(I18n.GetString("import.password.invalid"));
                }
            }
            return importer.Parse(path, password);
        }

        private void ShowImportResult(int total, int duplicate, int tokenID)
        {
            if (total == 0)
            {
                throw new Exception(I18n.GetString("import.notokens"));
            }

            if (total == 1)
            {
                if (duplicate == 1)
                {
                    throw new Exception(I18n.GetString("import.duplicate"));
                }
                NavigationContextManager.CurrentContext["type"] = "added";
                NavigationContextManager.CurrentContext["tokenID"] = tokenID;
            }
            else
            {
                NavigationContextManager.CurrentContext["type"] = "added-multiple";
                NavigationContextManager.CurrentContext["tokenID"] = 0;
                NavigationContextManager.CurrentContext["count"] = total;
                NavigationContextManager.CurrentContext["duplicateCount"] = duplicate;
            }

            mainWindow.Navigate(typeof(TokenSuccessPage));
        }
    }
}
