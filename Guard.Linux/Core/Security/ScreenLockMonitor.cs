using Avalonia.Threading;
using Guard.Core;
using Guard.Core.Security;
using Guard.Core.Storage;
using Tmds.DBus.Protocol;

namespace Guard.Linux.Core.Security
{
    /// <summary>
    /// Locks the app when the desktop session is locked. Listens to the screensaver
    /// interfaces of GNOME and freedesktop compatible desktops and to systemd-logind.
    /// </summary>
    internal static class ScreenLockMonitor
    {
        private static Action? onLock;

        internal static void Register(Action lockAction)
        {
            onLock = lockAction;
            _ = RegisterSessionBus();
            _ = RegisterLogind();
        }

        private static void OnScreenLocked(string source)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!Auth.IsLoggedIn())
                {
                    return;
                }
                if (
                    SettingsManager.Settings.LockOnScreenLock
                    || RegistrySettings.ForceLockOnScreenLock()
                )
                {
                    Log.Logger.Information("Locking app because the screen was locked ({0})", source);
                    onLock?.Invoke();
                }
            });
        }

        private static async Task RegisterSessionBus()
        {
            try
            {
                Connection connection = await DBus.GetSession();
                foreach (string iface in new[] { "org.gnome.ScreenSaver", "org.freedesktop.ScreenSaver" })
                {
                    await connection.AddMatchAsync(
                        new MatchRule
                        {
                            Type = MessageType.Signal,
                            Interface = iface,
                            Member = "ActiveChanged",
                        },
                        (Message message, object? _) => message.GetBodyReader().ReadBool(),
                        (Exception? ex, bool active, object? _, object? _) =>
                        {
                            if (ex == null && active)
                            {
                                OnScreenLocked(iface);
                            }
                        },
                        ObserverFlags.None,
                        null,
                        null,
                        false
                    );
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Could not subscribe to screensaver signals: {0}", ex.Message);
            }
        }

        private static async Task RegisterLogind()
        {
            try
            {
                Connection connection = await DBus.GetSystem();
                string? sessionPath = await GetLogindSessionPath(connection);
                if (sessionPath == null)
                {
                    Log.Logger.Warning("Could not determine logind session");
                    return;
                }
                await connection.AddMatchAsync(
                    new MatchRule
                    {
                        Type = MessageType.Signal,
                        Interface = "org.freedesktop.login1.Session",
                        Member = "Lock",
                        Path = sessionPath,
                    },
                    (Message _, object? _) => true,
                    (Exception? ex, bool _, object? _, object? _) =>
                    {
                        if (ex == null)
                        {
                            OnScreenLocked("logind");
                        }
                    },
                    ObserverFlags.None,
                    null,
                    null,
                    false
                );
            }
            catch (Exception ex)
            {
                Log.Logger.Warning("Could not subscribe to logind lock signal: {0}", ex.Message);
            }
        }

        private static async Task<string?> GetLogindSessionPath(Connection connection)
        {
            try
            {
                MessageBuffer buffer;
                using (var writer = connection.GetMessageWriter())
                {
                    writer.WriteMethodCallHeader(
                        "org.freedesktop.login1",
                        "/org/freedesktop/login1",
                        "org.freedesktop.login1.Manager",
                        "GetSessionByPID",
                        "u",
                        MessageFlags.None
                    );
                    writer.WriteUInt32((uint)Environment.ProcessId);
                    buffer = writer.CreateMessage();
                }
                return await connection.CallMethodAsync(
                    buffer,
                    (Message m, object? _) => m.GetBodyReader().ReadObjectPathAsString(),
                    null
                );
            }
            catch (DBusException) { }

            string? sessionId = Environment.GetEnvironmentVariable("XDG_SESSION_ID");
            if (string.IsNullOrEmpty(sessionId))
            {
                return null;
            }
            MessageBuffer buffer2;
            using (var writer = connection.GetMessageWriter())
            {
                writer.WriteMethodCallHeader(
                    "org.freedesktop.login1",
                    "/org/freedesktop/login1",
                    "org.freedesktop.login1.Manager",
                    "GetSession",
                    "s",
                    MessageFlags.None
                );
                writer.WriteString(sessionId);
                buffer2 = writer.CreateMessage();
            }
            return await connection.CallMethodAsync(
                buffer2,
                (Message m, object? _) => m.GetBodyReader().ReadObjectPathAsString(),
                null
            );
        }
    }
}
