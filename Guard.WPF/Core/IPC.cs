using System.IO.Pipes;
using System.Windows;
using Guard.Core;
using Guard.Core.Storage;

namespace Guard.WPF.Core
{
    internal class IPC
    {
        internal static void InitPipeServer()
        {
            MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

            var pipeServerThread = new Thread(() =>
            {
                using var pipeServer = new NamedPipeServerStream(
                    $"{InstallationContext.GetMutexName()}-Start-Pipe"
                );

                while (true)
                {
                    pipeServer.WaitForConnection();
                    // When a connection is received, bring the app to the front
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
                        mainWindow.Show();
                        mainWindow.WindowState = WindowState.Normal;
                        mainWindow.Activate();

                        // Re-add tray icon if enabled to ensure it's visible (e.g. after a explorer.exe restart)
                        if (SettingsManager.Settings.MinimizeToTray)
                        {
                            mainWindow.RemoveTrayIcon();
                            mainWindow.AddTrayIcon();
                        }
                    });
                    pipeServer.Disconnect();
                }
            })
            {
                IsBackground = true
            };
            pipeServerThread.Start();
        }

        internal static bool SendToFront()
        {
            try
            {
                using var pipeClient = new NamedPipeClientStream(
                    $"{InstallationContext.GetMutexName()}-Start-Pipe"
                );
                pipeClient.Connect(1000);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
