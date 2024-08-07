using System.ComponentModel;
using Guard.CLI.Core;
using Guard.Core.Models;
using Guard.Core.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Guard.CLI.Commands
{
    internal sealed class ListCommand : AsyncCommand<ListCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Plain output without any formatting.")]
            [CommandOption("-p|--plain")]
            [DefaultValue(false)]
            public bool Plain { get; init; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            await Lifecycle.Init();

            List<DBTOTPToken> dbTokens = Database.GetAllTokens();
            List<TOTPTokenHelper> tokens = new List<TOTPTokenHelper>();
            foreach (DBTOTPToken dbToken in dbTokens)
            {
                tokens.Add(new TOTPTokenHelper(dbToken));
            }

            if (settings.Plain)
            {
                foreach (TOTPTokenHelper token in tokens)
                {
                    AnsiConsole.WriteLine(
                        $"{token.dBToken.Id} {token.dBToken.Issuer} {token.Username}"
                    );
                }
            }
            else
            {
                var table = new Table();
                table.AddColumn("ID");
                table.AddColumn("Issuer");
                table.AddColumn("Username");
                table.Border(TableBorder.Rounded);

                foreach (TOTPTokenHelper token in tokens)
                {
                    table.AddRow(
                        token.dBToken.Id.ToString(),
                        token.dBToken.Issuer,
                        token.Username ?? ""
                    );
                }
                AnsiConsole.Write(table);
            }

            return 0;
        }
    }
}
