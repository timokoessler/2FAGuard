using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Media;
using Avalonia.Layout;
using Guard.Avalonia.ViewModels;
using Guard.Avalonia.Views.Dialogs;
using Guard.Core;
using Guard.Core.Import;
using Guard.Core.Import.Importer;

namespace Guard.Avalonia.Views.Pages.Start;

public partial class ImportPage : UserControl
{
    public ImportPage()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.NavigateTo(new WelcomePage());
        }
    }

    private async void OnAegisImportClick(object? sender, RoutedEventArgs e)
    {
        await Import(new AegisAuthenticatorImporter());
    }

    private async void OnBitwardenImportClick(object? sender, RoutedEventArgs e)
    {
        await Import(new BitwardenImporter());
    }

    private async void OnTwoFASImportClick(object? sender, RoutedEventArgs e)
    {
        await Import(new TwoFasImporter());
    }

    private async void OnAuthenticatorProImportClick(object? sender, RoutedEventArgs e)
    {
        await Import(new AuthenticatorProImporter());
    }

    private async void OnProtonImportClick(object? sender, RoutedEventArgs e)
    {
        await Import(new ProtonAuthenticatorImporter());
    }

    private async void OnUriListImportClick(object? sender, RoutedEventArgs e)
    {
        await Import(new UriListImporter());
    }

    private async void OnQrScreenClick(object? sender, RoutedEventArgs e)
    {
        // QR Screen capture not implemented yet - would need screen capture permission
        await ShowMessage("QR Code from Screen", "Screen capture import is not yet implemented on Linux.");
    }

    private async Task Import(IImporter importer)
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            int total = 0, duplicate = 0, tokenID = 0;

            if (importer.Type == IImporter.ImportType.File)
            {
                var filter = GetFileTypeFilter(importer.SupportedFileExtensions);
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = $"Select {importer.Name} export file",
                    AllowMultiple = false,
                    FileTypeFilter = filter != null ? new[] { filter } : Array.Empty<FilePickerFileType>()
                });

                if (files.Count == 0) return;
                var file = files[0];

                byte[]? password = null;
                if (importer.RequiresPassword(file.Path.LocalPath))
                {
                    var dialog = new PasswordDialog(I18n.GetString("import.password"));
                    await dialog.ShowDialog(GetWindow(topLevel));
                    if (!dialog.Result)
                        return;
                    password = dialog.GetPassword();
                    if (password == null || password.Length == 0)
                    {
                        throw new Exception(I18n.GetString("import.password.invalid"));
                    }
                }

                (total, duplicate, tokenID) = importer.Parse(file.Path.LocalPath, password);
            }
            else if (importer.Type == IImporter.ImportType.Clipboard)
            {
                // Clipboard import
                if (importer.RequiresPassword(""))
                {
                    throw new NotImplementedException("Importing password encrypted content from clipboard is not yet supported");
                }
                (total, duplicate, tokenID) = importer.Parse(null, null);
            }
            else if (importer.Type == IImporter.ImportType.ScreenCapture)
            {
                if (importer.RequiresPassword(""))
                {
                    throw new NotImplementedException("Importers that require a password are not supported for screen capture");
                }
                (total, duplicate, tokenID) = importer.Parse(null, null);
            }
            else
            {
                throw new Exception("Invalid Importer Type");
            }

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
                // Navigate to success page
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.NavigateTo(new Add.TokenSuccessPage(tokenID));
                }
            }
            else
            {
                // Multiple tokens added
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.NavigateTo(new Add.TokenSuccessPage(0, total, duplicate));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Import of {importer} failed: {message} {stacktrace}", 
                importer.Name, ex.Message, ex.StackTrace);
            
            string content = I18n.GetString("import.failed.content") + " ";

            if (ex.Message.Contains("Decryption failed (null)"))
            {
                content = I18n.GetString("import.password.invalid");
            }
            else
            {
                content += ex.Message;
            }

            await ShowMessage(I18n.GetString("import.failed.title"), content);
        }
    }

    private FilePickerFileType? GetFileTypeFilter(string filter)
    {
        // Parse "Aegis Authenticator Export (*.json) | *.json" format
        var parts = filter.Split('|');
        if (parts.Length >= 2)
        {
            var extensions = parts[1].Trim().Split(',', ';')
                .Select(e => e.Trim().Replace("*.", ""))
                .Where(e => !string.IsNullOrEmpty(e))
                .ToArray();
            
            return new FilePickerFileType(parts[0].Trim())
            {
                Patterns = extensions
            };
        }
        return null;
    }

    private Window GetWindow(TopLevel topLevel)
    {
        return (topLevel as Window) ?? new Window();
    }

    private async Task ShowMessage(string title, string content)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 15,
                Children =
                {
                    new TextBlock { Text = content, TextWrapping = TextWrapping.Wrap },
                    new Button 
                    { 
                        Content = I18n.GetString("dialog.close"),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Classes = { "accent" }
                    }
                }
            }
        };
        
        var btn = ((StackPanel)dialog.Content).Children[1] as Button;
        btn!.Click += (s, e) => dialog.Close();
        
        await dialog.ShowDialog(GetWindow(topLevel));
    }
}