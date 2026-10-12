namespace Guard.Core.Platform;

public interface ISecureStorage
{
    string? Get(string service, string account);
    void Set(string service, string account, string value);
    void Delete(string service, string account);
}

public interface IPlatformService
{
    string GetAppDataPath();
    Task<bool> EnableAutostartAsync(string appName, string executablePath);
    Task<bool> DisableAutostartAsync(string appName);
    Task<bool> IsAutostartEnabledAsync(string appName);
    Task<bool> IsBiometricAvailableAsync();
    Task<string?> AuthenticateWithBiometricsAsync(string reason);
    Task<string?> SignWithBiometricsAsync(string challenge);
    Task<bool> RegisterBiometricsAsync();
    Task UnregisterBiometricsAsync();
    void PreventScreenCapture(IntPtr windowHandle, bool prevent);
    Task<byte[]?> CaptureScreenAsync();
    string GetOsVersionString();
    bool IsPortable { get; set; }
    ISecureStorage GetSecureStorage();
}

public interface IBiometricService
{
    Task<bool> IsAvailableAsync();
    Task<bool> RegisterAsync(string accountName);
    Task<string?> SignChallengeAsync(string challenge);
    Task UnregisterAsync(string accountName);
}

public interface IWebAuthnService
{
    Task<bool> IsAvailableAsync();
    Task<PlatformCredentialAttestation?> CreateCredentialAsync(IntPtr window, string rpId, string userName, string userDisplayName);
    Task<PlatformAssertion?> GetAssertionAsync(IntPtr window, string rpId);
}

public record PlatformCredentialAttestation(string Id, byte[] PublicKey, byte[] AttestationObject);
public record PlatformAssertion(string Id, byte[] AuthenticatorData, byte[] Signature, byte[] UserHandle);