using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Guard.Linux.Core
{
    /// <summary>
    /// Avalonia does not expose the caps lock state, so it is derived from the typed letters
    /// and the state of the shift key.
    /// </summary>
    internal static class CapsLockDetector
    {
        internal static void Attach(TextBox box, Action<bool> onChanged)
        {
            bool shift = false;
            box.AddHandler(
                InputElement.KeyDownEvent,
                (_, e) => shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift),
                RoutingStrategies.Tunnel
            );
            box.AddHandler(
                InputElement.TextInputEvent,
                (_, e) =>
                {
                    if (string.IsNullOrEmpty(e.Text) || e.Text.Length != 1)
                    {
                        return;
                    }
                    char c = e.Text[0];
                    if (!char.IsLetter(c) || char.ToUpperInvariant(c) == char.ToLowerInvariant(c))
                    {
                        return;
                    }
                    onChanged(char.IsUpper(c) != shift);
                },
                RoutingStrategies.Tunnel
            );
        }
    }
}
