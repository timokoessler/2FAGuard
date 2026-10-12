using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Guard.Core.Platform;

namespace Guard.Core.Platform;

internal class FallbackSecureStorage : ISecureStorage
{
    private readonly string _storagePath;
    private readonly byte[] _key;
    
    public FallbackSecureStorage(string appDataPath)
    {
        _storagePath = Path.Combine(appDataPath, "secure-storage.dat");
        // Derive key from machine-specific data
        var machineId = Environment.MachineName + Environment.UserName;
        using var sha256 = SHA256.Create();
        _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineId));
    }
    
    public string? Get(string service, string account)
    {
        try
        {
            if (!File.Exists(_storagePath))
                return null;
                
            var encryptedData = File.ReadAllBytes(_storagePath);
            var decrypted = Decrypt(encryptedData);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted);
            
            if (dict != null && dict.TryGetValue($"{service}:{account}", out var value))
            {
                return value;
            }
        }
        catch { }
        return null;
    }
    
    public void Set(string service, string account, string value)
    {
        try
        {
            var dict = new Dictionary<string, string>();
            
            if (File.Exists(_storagePath))
            {
                var encryptedData = File.ReadAllBytes(_storagePath);
                var decrypted = Decrypt(encryptedData);
                dict = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted) 
                    ?? new Dictionary<string, string>();
            }
            
            dict[$"{service}:{account}"] = value;
            
            var json = JsonSerializer.Serialize(dict);
            var encrypted = Encrypt(Encoding.UTF8.GetBytes(json));
            File.WriteAllBytes(_storagePath, encrypted);
        }
        catch { }
    }
    
    public void Delete(string service, string account)
    {
        try
        {
            if (!File.Exists(_storagePath))
                return;
                
            var encryptedData = File.ReadAllBytes(_storagePath);
            var decrypted = Decrypt(encryptedData);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted);
            
            if (dict != null && dict.Remove($"{service}:{account}"))
            {
                var json = JsonSerializer.Serialize(dict);
                var encrypted = Encrypt(Encoding.UTF8.GetBytes(json));
                File.WriteAllBytes(_storagePath, encrypted);
            }
        }
        catch { }
    }
    
    private byte[] Encrypt(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
        }
        return ms.ToArray();
    }
    
    private string Decrypt(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        
        var iv = new byte[aes.IV.Length];
        Array.Copy(data, 0, iv, 0, iv.Length);
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(data, iv.Length, data.Length - iv.Length);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return sr.ReadToEnd();
    }
}