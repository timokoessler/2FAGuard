using System.IO.Pipes;
using System.Security.Principal;
using Guard.Core.CliBridge;

namespace Guard.CLI.Core
{
    internal static class CliDesktopBridgeClient
    {
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
                using StreamWriter writer = new(pipeClient) { AutoFlush = true };
                using StreamReader reader = new(pipeClient);

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
            catch
            {
                return CliBridgeResponse.Error(
                    CliBridgeErrorCode.Unavailable,
                    "Desktop bridge is unavailable."
                );
            }
        }
    }
}
