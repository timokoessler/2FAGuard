using System.Globalization;
using Guard.CLI.Commands;
using Guard.Core;
using Spectre.Console.Cli;

namespace Guard.CLI
{
    public class Program
    {
        public static int Main(string[] args)
        {
            InstallationType installationType = InstallationType.CLASSIC_INSTALLER;
#if PORTABLE
            installationType = InstallationType.CLASSIC_PORTABLE;
#endif

            // Todo support UWP

            InstallationContext.Init(
                installationType,
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
                config
                    .AddCommand<ListCommand>("list")
                    .WithDescription("List all two-factor authentication tokens.");
                config
                    .AddCommand<AboutCommand>("about")
                    .WithDescription("Show information about the application.");
            });

            return app.Run(args);
        }
    }
}
