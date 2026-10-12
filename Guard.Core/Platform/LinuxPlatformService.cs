using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Guard.Core.Platform;

namespace Guard.Core.Platform;

internal class LinuxPlatformService : IPlatformService
{
    private ISecureStorage? _secureStorage;
    public bool IsPortable { get; set; }
    
    public string GetAppDataPath()
    {
        if (IsPortable)
        {
            return Path.Combine(AppContext.BaseDirectory ?? throw new Exception("Could not get process directory"), "2FAGuard-Data");
        }
        
        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (!string.IsNullOrEmpty(xdgDataHome))
        {
            return Path.Combine(xdgDataHome, "2FAGuard");
        }
        
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "2FAGuard");
    }
    
    public Task<bool> EnableAutostartAsync(string appName, string executablePath)
    {
        try
        {
            var autostartDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "autostart");
            Directory.CreateDirectory(autostartDir);
            
            var desktopFile = Path.Combine(autostartDir, $"{appName}.desktop");
            var content = $@"[Desktop Entry]
Type=Application
Name={appName}
Exec={executablePath} --autostart
Hidden=false
NoDisplay=false
X-GNOME-Autostart-enabled=true
";
            File.WriteAllText(desktopFile, content);
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
            var autostartDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "autostart");
            var desktopFile = Path.Combine(autostartDir, $"{appName}.desktop");
            if (File.Exists(desktopFile))
            {
                File.Delete(desktopFile);
            }
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
    
    public Task<bool> IsAutostartEnabledAsync(string appName)
    {
        var autostartDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "autostart");
        var desktopFile = Path.Combine(autostartDir, $"{appName}.desktop");
        return Task.FromResult(File.Exists(desktopFile));
    }
    
    public Task<bool> IsBiometricAvailableAsync()
    {
        // Linux biometric support via fprintd or similar
        // Check if fprintd is available
        return Task.FromResult(File.Exists("/usr/bin/fprintd-verify") || File.Exists("/usr/libexec/fprintd"));
    }
    
    public async Task<string?> AuthenticateWithBiometricsAsync(string reason)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "fprintd-verify",
                    Arguments = $"-r \"{reason}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0 ? "verified" : null;
        }
        catch
        {
            return null;
        }
    }
    
    public Task<string?> SignWithBiometricsAsync(string challenge)
    {
        // Linux doesn't have a direct equivalent to Windows Hello signing
        // Could use libfprint or similar for challenge-response
        return Task.FromResult<string?>(null);
    }
    
    public Task<bool> RegisterBiometricsAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "fprintd-enroll",
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();
            return Task.FromResult(process.ExitCode == 0);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
    
    public Task UnregisterBiometricsAsync()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "fprintd-delete",
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit();
        }
        catch { }
        return Task.CompletedTask;
    }
    
    public void PreventScreenCapture(IntPtr windowHandle, bool prevent)
    {
        // On Linux, this would require Wayland/X11 specific implementation
        // For now, we'll leave it as a no-op
    }
    
    public Task<byte[]?> CaptureScreenAsync()
    {
        // Linux screen capture - would use portal APIs or similar
        return Task.FromResult<byte[]?>(null);
    }
    
    public string GetOsVersionString()
    {
        try
        {
            var osRelease = File.ReadAllText("/etc/os-release");
            var prettyName = osRelease.Split('\n')
                .FirstOrDefault(l => l.StartsWith("PRETTY_NAME="))?
                .Split('=')[1].Trim('"');
            return prettyName ?? $"Linux {Environment.OSVersion.Version}";
        }
        catch
        {
            return $"Linux {Environment.OSVersion.Version}";
        }
    }
    
    public ISecureStorage GetSecureStorage()
    {
        return _secureStorage ??= new LinuxSecureStorage();
    }
}