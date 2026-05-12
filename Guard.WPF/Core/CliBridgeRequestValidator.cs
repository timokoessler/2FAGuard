using System.IO;
using Guard.Core.CliBridge;
using Guard.WPF.Core.Installation;

namespace Guard.WPF.Core
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
#if DEBUG
            return true;
#else
            try
            {
                return File.Exists(path) && Updater.IsFileTrusted(path);
            }
            catch
            {
                return false;
            }
#endif
        }
    }
}
