using System;
using System.Diagnostics;
using Guard.Core.Platform;

namespace Guard.Core.Platform;

internal class MacSecureStorage : ISecureStorage
{
    // Uses macOS Keychain via security command
    
    public string? Get(string service, string account)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "security",
                    Arguments = $"find-generic-password -s \"{service}\" -a \"{account}\" -w",
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
                    FileName = "security",
                    Arguments = $"add-generic-password -s \"{service}\" -a \"{account}\" -w \"{value}\" -T \"\" -U",
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
    
    public void Delete(string service, string account)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "security",
                    Arguments = $"delete-generic-password -s \"{service}\" -a \"{account}\"",
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