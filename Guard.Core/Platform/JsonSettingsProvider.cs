using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Guard.Core.Models;

namespace Guard.Core.Platform;

public interface ISettingsProvider
{
    T? GetValue<T>(string key, T? defaultValue);
    void SetValue<T>(string key, T value);
    bool HasKey(string key);
    void DeleteKey(string key);
}

public class JsonSettingsProvider : ISettingsProvider
{
    private readonly string _filePath;
    private readonly Dictionary<string, object> _settings = [];
    private readonly object _lock = new();
    private bool _loaded;

    public JsonSettingsProvider(string filePath)
    {
        _filePath = filePath;
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        
        lock (_lock)
        {
            if (_loaded) return;
            
            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    if (dict != null)
                    {
                        foreach (var kvp in dict)
                        {
                            _settings[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch { }
            }
            _loaded = true;
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { }
    }

    public T? GetValue<T>(string key, T? defaultValue)
    {
        EnsureLoaded();
        lock (_lock)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is JsonElement element)
                    {
                        return element.Deserialize<T>();
                    }
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch { }
            }
            return defaultValue;
        }
    }

    public void SetValue<T>(string key, T value)
    {
        EnsureLoaded();
        lock (_lock)
        {
            _settings[key] = value!;
            Save();
        }
    }

    public bool HasKey(string key)
    {
        EnsureLoaded();
        lock (_lock)
        {
            return _settings.ContainsKey(key);
        }
    }

    public void DeleteKey(string key)
    {
        EnsureLoaded();
        lock (_lock)
        {
            _settings.Remove(key);
            Save();
        }
    }
}