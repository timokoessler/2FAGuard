using System.Text.Json;
using System.Text.Json.Serialization;

namespace Guard.Core.CliBridge
{
    public static class CliBridgeProtocolConstants
    {
        public const int Version = 1;
        public const string GetCodeOperation = "GetCode";

        public static string GetPipeName()
        {
            return $"{InstallationContext.GetMutexName()}-CliBridge-Pipe";
        }
    }

    public static class CliBridgeOperation
    {
        public const string GetCode = CliBridgeProtocolConstants.GetCodeOperation;
    }

    public static class CliBridgeErrorCode
    {
        public const string InvalidRequest = "invalid_request";
        public const string Locked = "locked";
        public const string RateLimited = "rate_limited";
        public const string TokenNotFound = "token_not_found";
        public const string MultipleTokensFound = "multiple_tokens_found";
        public const string Unauthorized = "unauthorized";
        public const string Unavailable = "unavailable";
        public const string UnsupportedOperation = "unsupported_operation";
        public const string UnsupportedVersion = "unsupported_version";
    }

    public class CliBridgeRequest
    {
        public int Version { get; set; }
        public string? Operation { get; set; }
        public string? IssuerOrId { get; set; }
        public int RequestingProcessId { get; set; }
        public string? RequestingProcessPath { get; set; }
    }

    public class CliBridgeResponse
    {
        public bool Success { get; set; }
        public string? Issuer { get; set; }
        public string? Code { get; set; }
        public int? RemainingSeconds { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        public static CliBridgeResponse Ok(string issuer, string code, int remainingSeconds)
        {
            return new CliBridgeResponse
            {
                Success = true,
                Issuer = issuer,
                Code = code,
                RemainingSeconds = remainingSeconds,
            };
        }

        public static CliBridgeResponse Error(string errorCode, string errorMessage)
        {
            return new CliBridgeResponse
            {
                Success = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
            };
        }
    }

    public static class CliBridgeProtocolValidator
    {
        public static string? GetRequestError(CliBridgeRequest request)
        {
            if (request.Version != CliBridgeProtocolConstants.Version)
            {
                return CliBridgeErrorCode.UnsupportedVersion;
            }
            if (request.Operation != CliBridgeOperation.GetCode)
            {
                return CliBridgeErrorCode.UnsupportedOperation;
            }
            if (string.IsNullOrWhiteSpace(request.IssuerOrId))
            {
                return CliBridgeErrorCode.InvalidRequest;
            }
            if (request.RequestingProcessId <= 0 || string.IsNullOrWhiteSpace(request.RequestingProcessPath))
            {
                return CliBridgeErrorCode.InvalidRequest;
            }
            return null;
        }
    }

    public static class CliBridgeSerializer
    {
        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static string SerializeRequest(CliBridgeRequest request)
        {
            return JsonSerializer.Serialize(request, jsonSerializerOptions);
        }

        public static CliBridgeRequest? DeserializeRequest(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<CliBridgeRequest>(json, jsonSerializerOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string SerializeResponse(CliBridgeResponse response)
        {
            return JsonSerializer.Serialize(response, jsonSerializerOptions);
        }

        public static CliBridgeResponse? DeserializeResponse(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<CliBridgeResponse>(json, jsonSerializerOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
