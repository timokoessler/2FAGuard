using Guard.Core;
using Guard.Core.CliBridge;
using Guard.Core.Security;
using Guard.WPF.Core.Installation;
using System.IO;

namespace Guard.WPF.Core.CliBridge
{
    internal static class CliBridgeRequestValidator
    {
        internal static string? GetError(CliBridgeRequest request, string? processPath)
        {
            string? requestError = CliBridgeProtocolValidator.GetRequestError(request);
            if (requestError != null)
            {
                return requestError;
            }

            if (processPath == null || !IsTrustedBinary(processPath))
            {
                return CliBridgeErrorCode.Unauthorized;
            }

            return null;
        }

        private static bool IsTrustedBinary(string path)
        {
            if(InstallationInfo.IsInDebugMode())
            {
                return true;
            }

            return TrustedExecutable.IsFileTrusted(path, strict: true);
        }
    }
}
