using System.Net.Sockets;
using System.Text;
using Guard.Core;
using Guard.Core.CliBridge;
using Guard.Core.Models;
using Guard.Core.Security;
using Guard.Core.Storage;

namespace Guard.Linux.Core.CliBridge
{
    /// <summary>
    /// Lets the CLI get TOTP codes from the running (unlocked) desktop app.
    /// Same protocol as the Windows app, but over a Unix domain socket.
    /// </summary>
    internal static class CliBridgeServer
    {
        private static readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(2);
        private static readonly CliBridgeRateLimiter rateLimiter = new(5, TimeSpan.FromSeconds(30));

        internal static void Init()
        {
            Socket listener;
            try
            {
                listener = UnixSocket.Listen(CliBridgeProtocolConstants.GetPipeName());
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Failed to start CLI bridge: {0}", ex.Message);
                return;
            }

            Thread thread = new(() =>
            {
                while (true)
                {
                    try
                    {
                        Socket client = listener.Accept();
                        _ = Task.Run(() => HandleConnection(client));
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error("CLI bridge listener failed: {0}", ex.Message);
                        Thread.Sleep(1000);
                    }
                }
            })
            {
                IsBackground = true,
                Name = "CliBridge",
            };
            thread.Start();
        }

        private static async Task HandleConnection(Socket client)
        {
            CliBridgeRequest? request = null;
            int processId = 0;
            string? processPath = null;
            CliBridgeResponse response = CliBridgeResponse.Error(
                CliBridgeErrorCode.Unavailable,
                "Desktop bridge request failed."
            );
            using NetworkStream stream = new(client, ownsSocket: true);
            try
            {
                var peer = UnixSocket.GetPeerCredentials(client);
                processId = peer.ProcessId;
                if (peer.UserId != UnixSocket.GetEffectiveUserId())
                {
                    response = CliBridgeResponse.Error(
                        CliBridgeErrorCode.Unauthorized,
                        "Desktop bridge request was rejected."
                    );
                    return;
                }
                if (!rateLimiter.Allow())
                {
                    response = CliBridgeResponse.Error(
                        CliBridgeErrorCode.RateLimited,
                        "Desktop bridge rate limit exceeded."
                    );
                    return;
                }

                using var timeout = new CancellationTokenSource(requestTimeout);
                using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
                string? requestJson = await reader.ReadLineAsync(timeout.Token);
                request =
                    requestJson == null ? null : CliBridgeSerializer.DeserializeRequest(requestJson);

                // The client waits for the response, so its PID cannot have been reused yet
                processPath = UnixSocket.GetProcessPath(processId);
                if (client.Poll(0, SelectMode.SelectRead) && client.Available == 0)
                {
                    throw new IOException("CLI bridge client disconnected during validation.");
                }

                response = HandleRequest(request, processPath);
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
                Log.Logger.Error("CLI bridge request failed: {0}", ex.Message);
            }
            finally
            {
                await WriteResponse(stream, response);
                Log.Logger.Information(
                    "CLI bridge request at {Time}: processId={ProcessId}, process={ProcessPath}, success={Success}, error={ErrorCode}",
                    DateTimeOffset.Now,
                    processId,
                    processPath ?? "",
                    response.Success,
                    response.ErrorCode ?? ""
                );
            }
        }

        private static async Task WriteResponse(NetworkStream stream, CliBridgeResponse response)
        {
            try
            {
                using var timeout = new CancellationTokenSource(requestTimeout);
                byte[] responseBytes = Encoding.UTF8.GetBytes(
                    CliBridgeSerializer.SerializeResponse(response) + "\n"
                );
                await stream.WriteAsync(responseBytes, timeout.Token);
            }
            catch (Exception ex)
            {
                Log.Logger.Error("CLI bridge response failed: {0}", ex.Message);
            }
        }

        private static CliBridgeResponse HandleRequest(CliBridgeRequest? request, string? processPath)
        {
            if (request == null)
            {
                return CliBridgeResponse.Error(
                    CliBridgeErrorCode.InvalidRequest,
                    "Invalid desktop bridge request."
                );
            }

            string? validationError = CliBridgeRequestValidator.GetError(request, processPath);
            if (validationError != null)
            {
                return CliBridgeResponse.Error(
                    validationError,
                    "Desktop bridge request was rejected."
                );
            }

            if (!Auth.IsLoggedIn())
            {
                return CliBridgeResponse.Error(CliBridgeErrorCode.Locked, "Desktop app is locked.");
            }

            CliBridgeTokenSelectionResult result = CliBridgeTokenSelector.Select(
                request.IssuerOrId ?? "",
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
    }
}
