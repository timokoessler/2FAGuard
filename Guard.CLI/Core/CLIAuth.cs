using System.Text;
using Guard.Core.Security;
using Spectre.Console;

namespace Guard.CLI.Core
{
    internal class CLIAuth
    {
        public static async Task<bool> Login(bool promptWinHello = true)
        {
            try
            {
                if (!Auth.IsLoginEnabled())
                {
                    Auth.LoginInsecure();
                }
                else if (promptWinHello && Auth.IsWindowsHelloRegistered())
                {
                    await AnsiConsole
                        .Status()
                        .StartAsync(
                            "Authenticate with Windows Hello...",
                            async ctx =>
                            {
                                await Auth.LoginWithWindowsHello();
                            }
                        );
                }
                else
                {
                    byte[] pass = Encoding.UTF8.GetBytes(
                        AnsiConsole.Prompt(new TextPrompt<string>("Enter your password: ").Secret())
                    );
                    Auth.LoginWithPassword(pass);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("UserCanceled"))
                {
                    return await Login(false);
                }
                AnsiConsole.MarkupLine("[red]Error:[/] " + ex.Message);
                return false;
            }
        }
    }
}
