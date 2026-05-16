using System.IO.Pipes;
using System.Security.Principal;
using Guard.Core.CliBridge;

namespace Guard.CLI.Core
{
    internal static class CliDesktopBridgeClient
    {
        // The client does not verify server authenticity. This is intentional: the only
        // data sent to the server is the issuer name or ID, which the user already typed
        // on the command line and is visible in the process argument list. A spoofed server
        // cannot extract anything the attacker does not already know, and a wrong TOTP code
        // returned by a spoofed server would simply fail at login.
        internal static async Task<CliBridgeResponse> GetCode(string issuerOrId)
        {
            try
            {
                CliBridgeRequest request = new()
                {
                    Version = CliBridgeProtocolConstants.Version,
                    Operation = CliBridgeOperation.GetCode,
                    IssuerOrId = issuerOrId,
                };

                using var pipeClient = new NamedPipeClientStream(
                    ".",
                    CliBridgeProtocolConstants.GetPipeName(),
                    PipeDirection.InOut,
                    PipeOptions.CurrentUserOnly,
                    TokenImpersonationLevel.Identification
                );

                await pipeClient.ConnectAsync(1000);
                StreamWriter writer = new(pipeClient) { AutoFlush = true };
                StreamReader reader = new(pipeClient);

                await writer.WriteLineAsync(CliBridgeSerializer.SerializeRequest(request));
                using var readTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                string? responseJson = await reader.ReadLineAsync(readTimeout.Token);
                if (responseJson == null)
                {
                    return CliBridgeResponse.Error(
                        CliBridgeErrorCode.Unavailable,
                        "Desktop app did not return a response."
                    );
                }

                return CliBridgeSerializer.DeserializeResponse(responseJson)
                    ?? CliBridgeResponse.Error(
                        CliBridgeErrorCode.Unavailable,
                        "Desktop app returned an invalid response."
                    );
            }
            catch (Exception e)
            {
                return CliBridgeResponse.Error(
                    CliBridgeErrorCode.Unavailable,
                    $"Desktop bridge is unavailable: {e.Message}"
                );
            }
        }
    }
}
