using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using Guard.Core;
using Guard.Core.CliBridge;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Core.Storage;

namespace Guard.WPF.Core
{
    internal static class CliBridgeServer
    {
        private const int MaxRequestBytes = 4096;
        private static readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(2);
        private static readonly CliBridgeRateLimiter rateLimiter = new(
            5,
            TimeSpan.FromSeconds(30)
        );

        internal static void Init()
        {
            Thread thread = new(() =>
            {
                try
                {
                    while (true)
                    {
                        NamedPipeServerStream pipeServer = new(
                            CliBridgeProtocolConstants.GetPipeName(),
                            PipeDirection.InOut,
                            NamedPipeServerStream.MaxAllowedServerInstances,
                            PipeTransmissionMode.Byte,
                            PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous
                        );
                        pipeServer.WaitForConnection();
                        _ = Task.Run(() => HandleConnection(pipeServer));
                    }
                }
                catch (Exception ex)
                {
                    Log.Logger.Error("CLI bridge listener failed: {0}", ex.Message);
                }
            })
            {
                IsBackground = true,
            };
            thread.Start();
        }

        private static async Task HandleConnection(NamedPipeServerStream pipeServer)
        {
            CliBridgeRequest? request = null;
            CliBridgeClientInfo clientInfo = new();
            CliBridgeResponse response = CliBridgeResponse.Error(
                CliBridgeErrorCode.Unavailable,
                "Desktop bridge request failed."
            );
            try
            {
                clientInfo = GetClientInfo(pipeServer);
                if (!rateLimiter.Allow())
                {
                    response = CliBridgeResponse.Error(
                        CliBridgeErrorCode.RateLimited,
                        "Desktop bridge rate limit exceeded."
                    );
                    return;
                }

                using var timeout = new CancellationTokenSource(requestTimeout);
                string? requestJson = await ReadLine(pipeServer, timeout.Token);
                request = requestJson == null
                    ? null
                    : CliBridgeSerializer.DeserializeRequest(requestJson);

                response = HandleRequest(request, clientInfo);
            }
            catch (OperationCanceledException)
            {
                response = CliBridgeResponse.Error(
                    CliBridgeErrorCode.InvalidRequest,
                    "Desktop bridge request timed out."
                );
            }
            catch (Exception ex)
            {
                response = CliBridgeResponse.Error(
                    CliBridgeErrorCode.Unavailable,
                    "Desktop bridge request failed."
                );
                Log.Logger.Error("CLI bridge request failed: {0}", ex.Message);
            }
            finally
            {
                await WriteResponse(pipeServer, response);
                LogRequest(request, clientInfo, response);
                pipeServer.Dispose();
            }
        }

        private static async Task<string?> ReadLine(Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1];
            using MemoryStream requestBytes = new();
            while (requestBytes.Length < MaxRequestBytes)
            {
                int read = await stream.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    return null;
                }
                if (buffer[0] == '\n')
                {
                    return Encoding.UTF8.GetString(requestBytes.ToArray()).TrimEnd('\r');
                }
                requestBytes.WriteByte(buffer[0]);
            }

            return null;
        }

        private static async Task WriteResponse(
            NamedPipeServerStream pipeServer,
            CliBridgeResponse response
        )
        {
            try
            {
                if (!pipeServer.IsConnected)
                {
                    return;
                }

                using var timeout = new CancellationTokenSource(requestTimeout);
                byte[] responseBytes = Encoding.UTF8.GetBytes(
                    CliBridgeSerializer.SerializeResponse(response) + "\n"
                );
                await pipeServer.WriteAsync(responseBytes, timeout.Token);
            }
            catch (Exception ex)
            {
                Log.Logger.Error("CLI bridge response failed: {0}", ex.Message);
            }
        }

        private static CliBridgeResponse HandleRequest(
            CliBridgeRequest? request,
            CliBridgeClientInfo clientInfo
        )
        {
            if (request == null)
            {
                return CliBridgeResponse.Error(
                    CliBridgeErrorCode.InvalidRequest,
                    "Invalid desktop bridge request."
                );
            }

            string? validationError = CliBridgeRequestValidator.GetError(
                request,
                clientInfo.ProcessPath
            );
            if (validationError != null)
            {
                return CliBridgeResponse.Error(
                    validationError,
                    "Desktop bridge request was rejected."
                );
            }

            if (!Auth.IsLoggedIn())
            {
                return CliBridgeResponse.Error(
                    CliBridgeErrorCode.Locked,
                    "Desktop app is locked."
                );
            }

            return GetCode(request.IssuerOrId ?? "");
        }

        private static CliBridgeClientInfo GetClientInfo(NamedPipeServerStream pipeServer)
        {
            if (!GetNamedPipeClientProcessId(pipeServer.SafePipeHandle, out uint clientProcessId))
            {
                throw new IOException("Could not get CLI bridge client process ID.");
            }

            int processId = checked((int)clientProcessId);
            using Process process = Process.GetProcessById(processId);
            return new CliBridgeClientInfo
            {
                ProcessId = processId,
                ProcessPath = process.MainModule?.FileName,
            };
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetNamedPipeClientProcessId(
            Microsoft.Win32.SafeHandles.SafePipeHandle pipe,
            out uint clientProcessId
        );

        private static CliBridgeResponse GetCode(string issuerOrId)
        {
            CliBridgeTokenSelectionResult result = CliBridgeTokenSelector.Select(
                issuerOrId,
                Database.GetTokenById,
                Database.GetTokensByIssuer
            );
            if (!result.Success || result.Token == null)
            {
                return CliBridgeResponse.Error(
                    result.ErrorCode ?? CliBridgeErrorCode.TokenNotFound,
                    result.ErrorMessage ?? "No token found."
                );
            }

            DBTOTPToken dbToken = result.Token;
            TOTPTokenHelper token = new(dbToken);
            return CliBridgeResponse.Ok(
                dbToken.Issuer,
                token.GenerateToken(),
                token.GetRemainingSeconds()
            );
        }

        private static void LogRequest(
            CliBridgeRequest? request,
            CliBridgeClientInfo clientInfo,
            CliBridgeResponse response
        )
        {
            Log.Logger.Information(
                "CLI bridge request at {Time}: processId={ProcessId}, process={ProcessPath}, target={Target}, success={Success}, error={ErrorCode}",
                DateTimeOffset.Now,
                clientInfo.ProcessId,
                clientInfo.ProcessPath ?? "",
                request?.IssuerOrId ?? "",
                response.Success,
                response.ErrorCode ?? ""
            );
        }

        private class CliBridgeClientInfo
        {
            public int ProcessId { get; set; }
            public string? ProcessPath { get; set; }
        }
    }
}
