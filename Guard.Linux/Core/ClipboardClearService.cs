using Avalonia.Threading;
using Guard.Core;
using Guard.Core.Models;

namespace Guard.Linux.Core
{
    internal static class ClipboardClearService
    {
        private static readonly Dictionary<string, CancellationTokenSource> _pending = [];

        internal static int GetClearClipboardMs(ClearClipboardSetting setting) =>
            setting switch
            {
                ClearClipboardSetting.TenSeconds => 10_000,
                ClearClipboardSetting.TwentySeconds => 20_000,
                ClearClipboardSetting.ThirtySeconds => 30_000,
                ClearClipboardSetting.OneMinute => 60_000,
                _ => 0,
            };

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

        /// <summary>
        /// Clears the clipboard if it still contains a copied token. Called on exit.
        /// </summary>
        internal static async Task ClearNow()
        {
            if (_pending.Count == 0)
            {
                return;
            }
            var texts = _pending.Keys.ToList();
            foreach (var cts in _pending.Values)
            {
                cts.Cancel();
            }
            _pending.Clear();

            try
            {
                string? current = await ClipboardHelper.GetText();
                if (current != null && texts.Contains(current))
                {
                    await ClipboardHelper.Clear();
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Failed to clear clipboard on exit: {0}", ex.Message);
            }
        }

        private static async Task ClearAfterDelay(string text, int ms, CancellationToken ct)
        {
            try
            {
                await Task.Delay(Math.Max(0, ms), ct);
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    try
                    {
                        _pending.Remove(text);
                        if (ct.IsCancellationRequested)
                        {
                            return;
                        }
                        if (await ClipboardHelper.GetText() == text)
                        {
                            await ClipboardHelper.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error("Failed to clear clipboard: {0}", ex.Message);
                    }
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log.Logger.Error("Unexpected error in clipboard clear: {0}", ex.Message);
            }
        }
    }
}
