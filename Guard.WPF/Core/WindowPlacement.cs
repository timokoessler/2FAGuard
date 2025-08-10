using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using Guard.Core;

namespace Guard.WPF.Core
{
    public static class WindowPlacement
    {
        private static readonly string placementFilePath = Path.Combine(
            InstallationContext.GetAppDataFolderPath(),
            "window-placement"
        );

        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IncludeFields = true,
        };

        public static void SavePlacement(Window window)
        {
            try
            {
                var windowHandle = NativeWindow.WindowToNativeHandle(window);

                NativeWindow.GetWindowPlacement(
                    windowHandle,
                    out NativeWindow.WINDOWPLACEMENT placement
                );

                string fileContent = JsonSerializer.Serialize(placement, jsonSerializerOptions);
                byte[] fileData = Encoding.UTF8.GetBytes(fileContent);
                File.WriteAllBytes(placementFilePath, fileData);
            }
            catch (Exception e)
            {
                Log.Logger.Error("Couldn't save position for window", e);
            }
        }

        public static void ApplyPlacement(Window window)
        {
            try
            {
                var windowHandle = NativeWindow.WindowToNativeHandle(window);
                if (!File.Exists(placementFilePath))
                {
                    return;
                }

                byte[] fileData = File.ReadAllBytes(placementFilePath);
                string fileContent = Encoding.UTF8.GetString(fileData);
                NativeWindow.WINDOWPLACEMENT placement =
                    JsonSerializer.Deserialize<NativeWindow.WINDOWPLACEMENT>(
                        fileContent,
                        jsonSerializerOptions
                    );

                placement.Length = Marshal.SizeOf(typeof(NativeWindow.WINDOWPLACEMENT));

                NativeWindow.SetWindowPlacement(windowHandle, ref placement);
            }
            catch (Exception e)
            {
                Log.Logger.Error("Couldn't apply position for window", e);
            }
        }
    }
}
