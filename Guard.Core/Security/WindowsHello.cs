using Guard.Core.Platform;

namespace Guard.Core.Security
{
    public static class BiometricAuth
    {
        private static string GetAccountName()
        {
            return $"2FAGuard{(InstallationContext.IsPortable() ? "Portable" : "")}{Auth.GetInstallationID()}";
        }

        public static async Task<bool> RegisterAsync()
        {
            return await PlatformService.Instance.RegisterBiometricsAsync();
        }

        public static async Task<bool> IsAvailableAsync()
        {
            return await PlatformService.Instance.IsBiometricAvailableAsync();
        }

        public static async Task<string?> GetSignedChallengeAsync()
        {
            var challenge = Auth.GetBiometricChallenge();
            return await PlatformService.Instance.SignWithBiometricsAsync(challenge);
        }

        public static async Task UnregisterAsync()
        {
            await PlatformService.Instance.UnregisterBiometricsAsync();
        }

        public static async Task<string?> AuthenticateAsync(string reason)
        {
            return await PlatformService.Instance.AuthenticateWithBiometricsAsync(reason);
        }
    }
}