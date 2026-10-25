using System.Net.Sockets;
using Avalonia.Threading;
using Guard.Core;
using Guard.Core.Storage;
using Guard.Linux.Core.CliBridge;

namespace Guard.Linux.Core
{
    /// <summary>
    /// Lets a second instance of the app bring the running instance to the front.
    /// </summary>
    internal class IPC
    {
        private static string GetStartSocketPath() => InstallationContext.GetPipeName("Start-Pipe");

        internal static void InitPipeServer(Action bringToFront)
        {
            Socket listener;
            try
            {
                listener = UnixSocket.Listen(GetStartSocketPath());
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Failed to start IPC server: {0}", ex.Message);
                return;
            }

            var thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        using Socket client = listener.Accept();
                        var peer = UnixSocket.GetPeerCredentials(client);
                        if (peer.UserId != UnixSocket.GetEffectiveUserId())
                        {
                            continue;
                        }
                        Dispatcher.UIThread.Post(bringToFront);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error("IPC server error: {0}", ex.Message);
                        Thread.Sleep(1000);
                    }
                }
            })
            {
                IsBackground = true,
                Name = "IPC",
            };
            thread.Start();

            if (RegistrySettings.IsCliDesktopBridgeEnabled())
            {
                CliBridgeServer.Init();
            }
        }

        internal static bool SendToFront()
        {
            try
            {
                using Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                var connectTask = socket.ConnectAsync(new UnixDomainSocketEndPoint(GetStartSocketPath()));
                return connectTask.Wait(1000);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
