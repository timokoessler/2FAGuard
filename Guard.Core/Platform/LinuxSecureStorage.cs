using System;
using System.Diagnostics;
using System.Text.Json;
using Guard.Core.Platform;

namespace Guard.Core.Platform;

internal class LinuxSecureStorage : ISecureStorage
{
    // Uses libsecret / secret-tool / keyring
    
    public string? Get(string service, string account)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "secret-tool",
                    Arguments = $"lookup service {service} account {account}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            
            return process.ExitCode == 0 && !string.IsNullOrEmpty(output) ? output : null;
        }
        catch
        {
            return null;
        }
    }
    
    public void Set(string service, string account, string value)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "secret-tool",
                    Arguments = $"store --label=\"2FAGuard {service}:{account}\" service {service} account {account}",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            process.StandardInput.Write(value);
            process.StandardInput.Close();
            process.WaitForExit();
        }
        catch { }
    }
    
    public void Delete(string service, string account)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "secret-tool",
                    Arguments = $"clear service {service} account {account}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            process.WaitForExit();
        }
        catch { }
    }
}