using Guard.Core.Models;
using Windows.ApplicationModel;

namespace Guard
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            App application = new();
            application.InitializeComponent();
            if (DesktopBridgeHelper.IsRunningAsUwp())
            {
                application.OnActivatedGuard(AppInstance.GetActivatedEventArgs());
            }
            application.Run();
        }
    }
}
