using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Guard.Core;

namespace Guard.Linux.Core
{
    internal static class ClipboardHelper
    {
        // Tells clipboard managers like KDE Klipper to not store the content in the history
        private const string PasswordManagerHint = "x-kde-passwordManagerHint";

        private static IClipboard GetClipboard()
        {
            return App.MainWindow?.Clipboard
                ?? throw new InvalidOperationException("Clipboard not available");
        }

        internal static async Task SetSecretText(string text)
        {
            IClipboard clipboard = GetClipboard();
            try
            {
#pragma warning disable CS0618
                DataObject data = new();
                data.Set(DataFormats.Text, text);
                data.Set(PasswordManagerHint, Encoding.UTF8.GetBytes("secret"));
                await clipboard.SetDataObjectAsync(data);
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Failed to set clipboard with hint: {0}", ex.Message);
                await clipboard.SetTextAsync(text);
            }
        }

        internal static Task SetText(string text)
        {
            return GetClipboard().SetTextAsync(text);
        }

        internal static Task<string?> GetText()
        {
            return GetClipboard().GetTextAsync();
        }

        internal static Task Clear()
        {
            return GetClipboard().ClearAsync();
        }

        internal static async Task<string[]> GetFormats()
        {
            return await GetClipboard().GetFormatsAsync();
        }

        internal static async Task<byte[]?> GetBytes(string format)
        {
#pragma warning disable CS0618
            object? data = await GetClipboard().GetDataAsync(format);
#pragma warning restore CS0618
            return data switch
            {
                byte[] bytes => bytes,
                string str => Encoding.UTF8.GetBytes(str),
                _ => null,
            };
        }

        internal static async Task SetPng(Bitmap bitmap)
        {
            using MemoryStream stream = new();
            bitmap.Save(stream);
#pragma warning disable CS0618
            DataObject data = new();
            data.Set("image/png", stream.ToArray());
            await GetClipboard().SetDataObjectAsync(data);
#pragma warning restore CS0618
        }
    }
}
