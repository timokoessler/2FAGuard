using Guard.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Guard.CLI.Commands
{
    internal class AboutCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            AnsiConsole.Write(new FigletText("2FAGuard"));
            AnsiConsole.WriteLine(
                $"Copyright © 2024-{DateTime.Now.Year} Timo Kössler and Open Source Contributors"
            );
            AnsiConsole.WriteLine(
                $"Version: {InstallationContext.GetVersionString()} ({InstallationContext.GetInstallationTypeString()})"
            );

            AnsiConsole.WriteLine("Free and Open Source software licensed under the MIT License.");
            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine("Website: [link=https://2faguard.app]2faguard.app[/]");
            AnsiConsole.MarkupLine(
                "GitHub: [link=https://github.com/timokoessler/2FAGuard]timokoessler/2FAGuard[/]"
            );
            AnsiConsole.MarkupLine(
                "Report a bug: [link=https://github.com/timokoessler/2FAGuard/issues/new/choose]Create a new issue[/]"
            );
            AnsiConsole.MarkupLine(
                "Privacy Policy: [link=https://2faguard.app/privacy]2faguard.app/privacy[/]"
            );
            AnsiConsole.MarkupLine(
                "Imprint: [link=https://2faguard.app/imprint]2faguard.app/imprint[/]"
            );

            return 0;
        }
    }
}
