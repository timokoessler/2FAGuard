using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Guard.Core.Platform;

namespace Guard.Core.Platform;

internal class WindowsPlatformService : IPlatformService
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
        // Windows Registry implementation
        try
        {
            using var rkApp = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            rkApp?.SetValue(appName, $"{executablePath} --autostart");
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
    
    public Task<bool> DisableAutostartAsync(string appName)
    {
        try
        {
            using var rkApp = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            rkApp?.DeleteValue(appName, false);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
    
    public Task<bool> IsAutostartEnabledAsync(string appName)
    {
        try
        {
            using var rkApp = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return Task.FromResult(rkApp?.GetValue(appName) != null);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
    
    public Task<bool> IsBiometricAvailableAsync()
    {
        // Windows Hello - requires Windows 10+
        return Task.FromResult(Environment.OSVersion.Version.Build >= 17763);
    }
    
    public Task<string?> AuthenticateWithBiometricsAsync(string reason)
    {
        // Windows Hello implementation would go here
        return Task.FromResult<string?>(null);
    }
    
    public Task<string?> SignWithBiometricsAsync(string challenge)
    {
        // Windows Hello implementation would go here
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
        // Windows API implementation
    }
    
    public Task<byte[]?> CaptureScreenAsync()
    {
        // Windows screen capture
        return Task.FromResult<byte[]?>(null);
    }
    
    public string GetOsVersionString()
    {
        if (Environment.OSVersion.Version.Build >= 22000)
            return $"Windows 11 {Environment.OSVersion.Version.Build}";
        return $"Windows 10 {Environment.OSVersion.Version.Build}";
    }
    
    public ISecureStorage GetSecureStorage()
    {
        return _secureStorage ??= new WindowsSecureStorage(GetAppDataPath());
    }
}