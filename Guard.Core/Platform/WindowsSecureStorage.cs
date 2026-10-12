using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Guard.Core.Platform;

namespace Guard.Core.Platform;

internal class WindowsSecureStorage : ISecureStorage
{
    // Uses Windows Credential Manager via Windows.Security.Credentials
    // This would require the Windows-specific implementation
    // For now, fallback to encrypted file storage
    
    private readonly string _storagePath;
    
    public WindowsSecureStorage(string appDataPath)
    {
        _storagePath = Path.Combine(appDataPath, "secure-storage.dat");
    }
    
    public string? Get(string service, string account)
    {
        return GetValue($"{service}:{account}");
    }
    
    public void Set(string service, string account, string value)
    {
        SetValue($"{service}:{account}", value);
    }
    
    public void Delete(string service, string account)
    {
        DeleteValue($"{service}:{account}");
    }
    
    private string? GetValue(string key)
    {
        try
        {
            if (!File.Exists(_storagePath))
                return null;
                
            var data = File.ReadAllBytes(_storagePath);
            var decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decrypted);
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            
            if (dict != null && dict.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        catch { }
        return null;
    }
    
    private void SetValue(string key, string value)
    {
        try
        {
            var dict = new Dictionary<string, string>();
            
            if (File.Exists(_storagePath))
            {
                var data = File.ReadAllBytes(_storagePath);
                var decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(decrypted);
                dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) 
                    ?? new Dictionary<string, string>();
            }
            
            dict[key] = value;
            
            var newJson = System.Text.Json.JsonSerializer.Serialize(dict);
            var newData = Encoding.UTF8.GetBytes(newJson);
            var encrypted = ProtectedData.Protect(newData, null, DataProtectionScope.CurrentUser);
            
            File.WriteAllBytes(_storagePath, encrypted);
        }
        catch { }
    }
    
    private void DeleteValue(string key)
    {
        try
        {
            if (!File.Exists(_storagePath))
                return;
                
            var data = File.ReadAllBytes(_storagePath);
            var decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decrypted);
            var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            
            if (dict != null && dict.Remove(key))
            {
                var newJson = System.Text.Json.JsonSerializer.Serialize(dict);
                var newData = Encoding.UTF8.GetBytes(newJson);
                var encrypted = ProtectedData.Protect(newData, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_storagePath, encrypted);
            }
        }
        catch { }
    }
}