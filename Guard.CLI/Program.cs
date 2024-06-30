using System.Globalization;
using Guard.CLI.Commands;
using Guard.Core;
using Guard.Core.Storage;
using Spectre.Console.Cli;

namespace Guard.CLI
{
    public class Program
    {
        public static int Main(string[] args)
        {
            // Todo: Detect InstallationType and isPortable and set it here
            InstallationType installationType = InstallationType.CLASSIC_PORTABLE;
            InstallationContext.Init(
                installationType,
                false,
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
                    ?? new Version(0, 0)
            );

            Log.Init();

            var app = new CommandApp();
            app.Configure(config =>
            {
                config.SetApplicationName("2fa");
                config.SetApplicationVersion(InstallationContext.GetVersionString());
                config.SetApplicationCulture(CultureInfo.GetCultureInfo("en"));
                config
                    .AddCommand<GetCodeCommand>("get")
                    .WithDescription("Get a two-factor authentication code.");
            });

            return app.Run(args);
        }
    }
}
