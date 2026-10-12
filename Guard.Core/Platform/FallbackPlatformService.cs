using System;
using System.IO;
using System.Threading.Tasks;
using Guard.Core.Platform;

namespace Guard.Core.Platform;

internal class FallbackPlatformService : IPlatformService
{
    private ISecureStorage? _secureStorage;
    public bool IsPortable { get; set; }
    
    public string GetAppDataPath()
    {
        if (IsPortable)
        {
            return Path.Combine(AppContext.BaseDirectory ?? throw new Exception("Could not get process directory"), "2FAGuard-Data");
        }
        
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "2FAGuard");
    }
    
    public Task<bool> EnableAutostartAsync(string appName, string executablePath)
    {
        return Task.FromResult(false);
    }
    
    public Task<bool> DisableAutostartAsync(string appName)
    {
        return Task.FromResult(false);
    }
    
    public Task<bool> IsAutostartEnabledAsync(string appName)
    {
        return Task.FromResult(false);
    }
    
    public Task<bool> IsBiometricAvailableAsync()
    {
        return Task.FromResult(false);
    }
    
    public Task<string?> AuthenticateWithBiometricsAsync(string reason)
    {
        return Task.FromResult<string?>(null);
    }
    
    public Task<string?> SignWithBiometricsAsync(string challenge)
    {
        return Task.FromResult<string?>(null);
    }
    
    public Task<bool> RegisterBiometricsAsync()
    {
        return Task.FromResult(false);
    }
    
    public Task UnregisterBiometricsAsync()
    {
        return Task.CompletedTask;
    }
    
    public void PreventScreenCapture(IntPtr windowHandle, bool prevent)
    {
    }
    
    public Task<byte[]?> CaptureScreenAsync()
    {
        return Task.FromResult<byte[]?>(null);
    }
    
    public string GetOsVersionString()
    {
        return $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
    }
    
    public ISecureStorage GetSecureStorage()
    {
        return _secureStorage ??= new FallbackSecureStorage(GetAppDataPath());
    }
}