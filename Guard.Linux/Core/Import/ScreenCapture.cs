using System.Diagnostics;
using Guard.Core;
using Tmds.DBus.Protocol;

namespace Guard.Linux.Core.Import
{
    /// <summary>
    /// Takes a screenshot of all screens. Uses the XDG Desktop Portal (works on Wayland and X11)
    /// and falls back to common command line tools.
    /// </summary>
    internal static class ScreenCapture
    {
        internal class ScreenshotDeniedException(string message) : Exception(message);

        internal static async Task<byte[]> CaptureAllScreens()
        {
            try
            {
                string? path = await CaptureWithPortal();
                if (path != null)
                {
                    return await ReadAndDelete(path);
                }
            }
            catch (ScreenshotDeniedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Screenshot portal failed: {0}", ex.Message);
            }

            return await CaptureWithTools();
        }

        private static async Task<byte[]> ReadAndDelete(string path)
        {
            byte[] data = await File.ReadAllBytesAsync(path);
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Could not delete screenshot file: {0}", ex.Message);
            }
            return data;
        }

        private static async Task<string?> CaptureWithPortal()
        {
            Connection connection = await DBus.GetSession();
            string token = "guard" + Guid.NewGuid().ToString("N")[..12];
            string sender = (connection.UniqueName ?? "").TrimStart(':').Replace('.', '_');
            string requestPath = $"/org/freedesktop/portal/desktop/request/{sender}/{token}";

            var tcs = new TaskCompletionSource<(uint response, string? uri)>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            using IDisposable subscription = await connection.AddMatchAsync(
                new MatchRule
                {
                    Type = MessageType.Signal,
                    Interface = "org.freedesktop.portal.Request",
                    Member = "Response",
                    Path = requestPath,
                },
                (Message message, object? _) =>
                {
                    var reader = message.GetBodyReader();
                    uint response = reader.ReadUInt32();
                    var results = reader.ReadDictionaryOfStringToVariantValue();
                    string? uri = results.TryGetValue("uri", out var value) ? value.GetString() : null;
                    return (response, uri);
                },
                (Exception? ex, (uint response, string? uri) value, object? _, object? _) =>
                {
                    if (ex != null)
                    {
                        tcs.TrySetException(ex);
                    }
                    else
                    {
                        tcs.TrySetResult(value);
                    }
                },
                ObserverFlags.None,
                null,
                null,
                false
            );

            MessageBuffer buffer;
            using (var writer = connection.GetMessageWriter())
            {
                writer.WriteMethodCallHeader(
                    "org.freedesktop.portal.Desktop",
                    "/org/freedesktop/portal/desktop",
                    "org.freedesktop.portal.Screenshot",
                    "Screenshot",
                    "sa{sv}",
                    MessageFlags.None
                );
                writer.WriteString("");
                var dict = writer.WriteDictionaryStart();
                writer.WriteDictionaryEntryStart();
                writer.WriteString("handle_token");
                writer.WriteVariantString(token);
                writer.WriteDictionaryEntryStart();
                writer.WriteString("interactive");
                writer.WriteVariantBool(false);
                writer.WriteDictionaryEnd(dict);
                buffer = writer.CreateMessage();
            }
            await connection.CallMethodAsync(buffer);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2)));
            if (completed != tcs.Task)
            {
                throw new TimeoutException("Screenshot portal did not respond");
            }
            var (response, uri) = await tcs.Task;
            if (response != 0)
            {
                throw new ScreenshotDeniedException("Screenshot was canceled or denied");
            }
            if (uri == null)
            {
                return null;
            }
            return new Uri(uri).LocalPath;
        }

        private static async Task<byte[]> CaptureWithTools()
        {
            string file = Path.Combine(Path.GetTempPath(), $"2fa-guard-{Guid.NewGuid():N}.png");
            (string cmd, string[] args)[] tools =
            [
                ("grim", [file]),
                ("gnome-screenshot", ["-f", file]),
                ("spectacle", ["-b", "-n", "-f", "-o", file]),
                ("import", ["-window", "root", file]),
            ];

            foreach (var (cmd, args) in tools)
            {
                try
                {
                    var psi = new ProcessStartInfo(cmd)
                    {
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                    };
                    foreach (string arg in args)
                    {
                        psi.ArgumentList.Add(arg);
                    }
                    using Process? process = Process.Start(psi);
                    if (process == null)
                    {
                        continue;
                    }
                    await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(20));
                    if (process.ExitCode == 0 && File.Exists(file))
                    {
                        return await ReadAndDelete(file);
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Tool not installed
                }
                catch (Exception ex)
                {
                    Log.Logger.Warning("Screenshot with {0} failed: {1}", cmd, ex.Message);
                }
            }
            throw new Exception(
                "Could not take a screenshot. Please make sure xdg-desktop-portal is running."
            );
        }
    }
}
