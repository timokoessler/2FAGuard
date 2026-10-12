using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Guard.Core.Platform;

namespace Guard.Core.Platform;

internal class MacPlatformService : IPlatformService
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
        try
        {
            var launchAgentsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LaunchAgents");
            Directory.CreateDirectory(launchAgentsDir);
            
            var plistFile = Path.Combine(launchAgentsDir, $"com.2faguard.{appName}.plist");
            var content = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key>
    <string>com.2faguard.{appName}</string>
    <key>ProgramArguments</key>
    <array>
        <string>{executablePath}</string>
        <string>--autostart</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>
";
            File.WriteAllText(plistFile, content);
            
            // Load the launch agent
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "launchctl",
                    Arguments = $"load {plistFile}",
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
    
    public Task<bool> DisableAutostartAsync(string appName)
    {
        try
        {
            var launchAgentsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LaunchAgents");
            var plistFile = Path.Combine(launchAgentsDir, $"com.2faguard.{appName}.plist");
            
            if (File.Exists(plistFile))
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "launchctl",
                        Arguments = $"unload {plistFile}",
                        UseShellExecute = false
                    }
                };
                process.Start();
                process.WaitForExit();
                
                File.Delete(plistFile);
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
        var launchAgentsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LaunchAgents");
        var plistFile = Path.Combine(launchAgentsDir, $"com.2faguard.{appName}.plist");
        return Task.FromResult(File.Exists(plistFile));
    }
    
    public Task<bool> IsBiometricAvailableAsync()
    {
        // Touch ID / Face ID on macOS
        return Task.FromResult(true); // Most modern Macs have Touch ID
    }
    
    public Task<string?> AuthenticateWithBiometricsAsync(string reason)
    {
        // macOS LocalAuthentication framework would be used here
        // For now, return null as it requires native interop
        return Task.FromResult<string?>(null);
    }
    
    public Task<string?> SignWithBiometricsAsync(string challenge)
    {
        return Task.FromResult<string?>(null);
    }
    
    public Task<bool> RegisterBiometricsAsync()
    {
        return Task.FromResult(true); // Touch ID is system-managed
    }
    
    public Task UnregisterBiometricsAsync()
    {
        return Task.CompletedTask;
    }
    
    public void PreventScreenCapture(IntPtr windowHandle, bool prevent)
    {
        // macOS implementation would use CGWindowListCreateImage or similar
    }
    
    public Task<byte[]?> CaptureScreenAsync()
    {
        return Task.FromResult<byte[]?>(null);
    }
    
    public string GetOsVersionString()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sw_vers",
                    Arguments = "-productVersion",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            var version = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return $"macOS {version}";
        }
        catch
        {
            return $"macOS {Environment.OSVersion.Version}";
        }
    }
    
    public ISecureStorage GetSecureStorage()
    {
        return _secureStorage ??= new MacSecureStorage();
    }
}