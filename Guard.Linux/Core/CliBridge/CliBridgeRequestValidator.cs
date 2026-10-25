using Guard.Core.CliBridge;
using Guard.WPF.Core.Installation;

namespace Guard.Linux.Core.CliBridge
{
    internal static class CliBridgeRequestValidator
    {
        private const string CliExecutableName = "2fa";

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

        /// <summary>
        /// Only the CLI that is shipped together with this app is trusted.
        /// </summary>
        private static bool IsTrustedBinary(string path)
        {
            if (InstallationInfo.IsInDebugMode())
            {
                return true;
            }

            string appDir = Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(AppContext.BaseDirectory)
            );
            string? binaryDir = Path.GetDirectoryName(path);
            return binaryDir != null
                && string.Equals(binaryDir, appDir, StringComparison.Ordinal)
                && string.Equals(Path.GetFileName(path), CliExecutableName, StringComparison.Ordinal);
        }
    }
}
