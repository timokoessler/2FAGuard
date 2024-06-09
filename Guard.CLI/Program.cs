using Guard.CLI.Commands;
using Guard.Core;
using Spectre.Console.Cli;
using System.Globalization;

namespace Guard.CLI
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // Todo: Detect InstallationType and isPortable and set it here
            InstallationContext.Init(InstallationType.CLASSIC_PORTABLE, false, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0));

            var app = new CommandApp();
            app.Configure(config =>
            {
                config.SetApplicationName("2fa");
                config.SetApplicationVersion(InstallationContext.GetVersionString());
                config.SetApplicationCulture(CultureInfo.GetCultureInfo("en"));
                config.AddCommand<GetCodeCommand>("get")
                    .WithDescription("Get a two-factor authentication code.");
            });
            
            return app.Run(args);
        }
    }
}