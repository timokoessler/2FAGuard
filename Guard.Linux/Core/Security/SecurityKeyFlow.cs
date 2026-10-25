using Guard.Core.Security.Linux;
using Guard.WPF.Core;

namespace Guard.Linux.Core.Security
{
    /// <summary>
    /// UI flow for FIDO2 security keys: select the device and ask for the PIN.
    /// </summary>
    internal static class SecurityKeyFlow
    {
        internal class CanceledException() : Exception("Canceled");

        internal static async Task<(Fido2DeviceInfo device, string? pin)> PrepareDevice()
        {
            List<Fido2DeviceInfo> devices = await Task.Run(Fido2.ListDevices);
            if (devices.Count == 0)
            {
                throw new Exception(I18n.GetString("linux.fido2.nodevice"));
            }

            Fido2DeviceInfo device = devices[0];
            if (devices.Count > 1)
            {
                int index = await Dialogs.SelectDialog(
                    I18n.GetString("linux.fido2.select"),
                    devices.Select(d => d.Name).ToList()
                );
                if (index < 0)
                {
                    throw new CanceledException();
                }
                device = devices[index];
            }

            if (!device.HasPin && !device.HasBuiltInUv)
            {
                throw new Exception(I18n.GetString("linux.fido2.nouv"));
            }

            string? pin = null;
            if (device.HasPin && !device.HasBuiltInUv)
            {
                pin = await Dialogs.TextInputDialog(
                    I18n.GetString("linux.fido2.pin.title"),
                    I18n.GetString("linux.fido2.pin.content"),
                    I18n.GetString("linux.fido2.pin.input"),
                    isPassword: true
                );
                if (pin == null)
                {
                    throw new CanceledException();
                }
            }
            return (device, pin);
        }

        internal static bool IsCanceled(Exception ex)
        {
            return ex is CanceledException
                || ex is Fido2Exception { IsCanceled: true }
                || ex.Message == "Canceled";
        }
    }
}
