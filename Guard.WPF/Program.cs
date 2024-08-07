using Guard.WPF.Core.Installation;
using Windows.ApplicationModel;

namespace Guard.WPF
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
