using Spectre.Console;
using Spectre.Console.Cli;

namespace Guard.CLI.Commands
{
    internal sealed class GetCodeCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            AnsiConsole.WriteLine("Hello from GetCodeCommand!");
            return 0;
        }
    }
}
