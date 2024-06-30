using System.ComponentModel;
using Guard.CLI.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Guard.CLI.Commands
{
    internal sealed class GetCodeCommand : AsyncCommand<GetCodeCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Name or ID of the token to get the code for.")]
            [CommandArgument(0, "[name / id]")]
            public string? NameOrId { get; init; }

            [Description("Copy the code to the clipboard instead of displaying it.")]
            [CommandOption("-c|--copy")]
            [DefaultValue(false)]
            public bool Copy { get; init; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (settings.NameOrId is null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No token name or ID specified.");
                return 1;
            }

            await Lifecycle.Init();

            return 0;
        }
    }
}
