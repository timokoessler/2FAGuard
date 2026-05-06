using System.ComponentModel;
using System.Diagnostics;
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
                using Process currentProcess = Process.GetCurrentProcess();
                string? processPath = currentProcess.MainModule?.FileName;
                if (processPath == null)
                {
                    return CliBridgeResponse.Error(
                        CliBridgeErrorCode.Unavailable,
                        "Could not get CLI process path."
                    );
                }

                CliBridgeRequest request = new()
                {
                    Version = CliBridgeProtocolConstants.Version,
                    Operation = CliBridgeOperation.GetCode,
                    IssuerOrId = issuerOrId,
                    RequestingProcessId = currentProcess.Id,
                    RequestingProcessPath = processPath,
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
                string? responseJson = await reader.ReadLineAsync();
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
            catch (Exception ex) when (
                ex is IOException
                || ex is InvalidOperationException
                || ex is TimeoutException
                || ex is UnauthorizedAccessException
                || ex is Win32Exception
            )
            {
                return CliBridgeResponse.Error(
                    CliBridgeErrorCode.Unavailable,
                    "Desktop bridge is unavailable."
                );
            }
        }
    }
}
