using System.ComponentModel;
using Guard.CLI.Core;
using Guard.Core.Models;
using Guard.Core.Storage;
using Spectre.Console;
using Spectre.Console.Cli;
using Windows.ApplicationModel.DataTransfer;

namespace Guard.CLI.Commands
{
    internal sealed class GetCodeCommand : AsyncCommand<GetCodeCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Issuer or ID of the token to get the code for.")]
            [CommandArgument(0, "[issuer / id]")]
            public string? IssuerOrId { get; init; }

            [Description("Copy the code to the clipboard instead of displaying it.")]
            [CommandOption("-c|--copy")]
            [DefaultValue(false)]
            public bool Copy { get; init; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (settings.IssuerOrId is null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No token issuer or ID specified.");
                return 1;
            }

            await Lifecycle.Init();

            DBTOTPToken dbToken;

            if (int.TryParse(settings.IssuerOrId, out int id))
            {
                dbToken = Database.GetTokenById(id);
                if (dbToken is null)
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] No token found with the specified ID.");
                    return 1;
                }
            }
            else
            {
                List<DBTOTPToken> dbTokens = Database.GetTokensByIssuer(settings.IssuerOrId);
                if (dbTokens.Count > 1)
                {
                    // Todo: Support selection if interactive or print ids?
                    AnsiConsole.MarkupLine(
                        "[red]Error:[/] Multiple tokens found with the specified issuer. Use the token ID instead."
                    );
                    return 1;
                }
                if (dbTokens.Count == 0)
                {
                    AnsiConsole.MarkupLine(
                        "[red]Error:[/] No token found with the specified issuer."
                    );
                    return 1;
                }
                dbToken = dbTokens[0];
            }

            TOTPTokenHelper token = new TOTPTokenHelper(dbToken);

            if (settings.Copy)
            {
                try
                {
                    Thread thread = new Thread(() =>
                    {
                        DataPackage package = new DataPackage();
                        package.SetText(token.GenerateToken());
                        Clipboard.SetContent(package);
                        Thread.Sleep(60); // Not working without this :(
                    });
                    thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                    thread.Start();
                    thread.Join();

                    AnsiConsole.MarkupLine(
                        $"Code for {dbToken.Issuer} copied to clipboard. It's still valid for {token.GetRemainingSeconds()} seconds."
                    );
                    return 0;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine(
                        $"[red]Error:[/] Failed to copy code to clipboard: {ex}"
                    );
                    return 1;
                }
            }

            AnsiConsole.MarkupLine($"Token for {dbToken.Issuer}: [bold]{token.GenerateToken()}[/]");
            AnsiConsole.MarkupLine($"It's still valid for {token.GetRemainingSeconds()} seconds.");

            return 0;
        }
    }
}
