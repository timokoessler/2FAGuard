using System.Windows;
using Guard.Core;

namespace Guard.WPF.Core
{
    internal static class ClipboardClearService
    {
        private static readonly Dictionary<string, CancellationTokenSource> _pending = [];

        internal static void Schedule(string text, int delayMs)
        {
            if (_pending.TryGetValue(text, out var existingCts))
            {
                existingCts.Cancel();
            }
            var cts = new CancellationTokenSource();
            _pending[text] = cts;
            _ = ClearAfterDelay(text, delayMs, cts.Token);
        }

        internal static void ClearNow()
        {
            if (_pending.Count == 0)
            {
                return;
            }
            var texts = _pending.Keys.ToList();
            foreach (var cts in _pending.Values)
            {
                try
                {
                    cts.Cancel();
                }
                catch (Exception ex)
                {
                    Log.Logger.Error("Failed to cancel clipboard clear timer: {0}", ex.Message);
                }
            }
            _pending.Clear();

            try
            {
                if (Clipboard.ContainsText() && texts.Contains(Clipboard.GetText()))
                {
                    Clipboard.Clear();
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Failed to clear clipboard on exit: {0}", ex.Message);
            }
            _ = RemoveFromClipboardHistoryAsync(texts);
        }

        private static async Task ClearAfterDelay(string text, int ms, CancellationToken ct)
        {
            try
            {
                await Task.Delay(Math.Max(0, ms), ct);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        _pending.Remove(text);
                        if (
                            !ct.IsCancellationRequested
                            && Clipboard.ContainsText()
                            && Clipboard.GetText() == text
                        )
                        {
                            Clipboard.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error("Failed to clear clipboard: {0}", ex.Message);
                    }
                });
                await RemoveFromClipboardHistoryAsync([text]);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log.Logger.Error("Unexpected error in clipboard clear: {0}", ex.Message);
            }
        }

        private static async Task RemoveFromClipboardHistoryAsync(IReadOnlyList<string> texts)
        {
            try
            {
                var result =
                    await Windows.ApplicationModel.DataTransfer.Clipboard.GetHistoryItemsAsync();
                if (
                    result.Status
                    != Windows
                        .ApplicationModel
                        .DataTransfer
                        .ClipboardHistoryItemsResultStatus
                        .Success
                )
                {
                    return;
                }
                foreach (var item in result.Items)
                {
                    if (
                        item.Content.Contains(
                            Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text
                        )
                    )
                    {
                        string itemText = await item.Content.GetTextAsync();
                        if (texts.Contains(itemText))
                        {
                            Windows.ApplicationModel.DataTransfer.Clipboard.DeleteItemFromHistory(
                                item
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Failed to remove token from clipboard history: {0}", ex.Message);
            }
        }
    }
}
