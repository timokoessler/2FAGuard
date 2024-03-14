using Guard.Core.Installation;
using System.Net.Http;
using System.Net.Http.Json;

namespace Guard.Core.Aptabase;

/*
 * MIT License
 * Copyright (c) 2023 Sumbit Labs Ltd.
 * https://github.com/aptabase/aptabase-maui
 * Small modifications were made to the original work.
 * */

/// <summary>
/// Aptabase client used for tracking events
/// </summary>
public interface IAptabaseClient
{
    void TrackEvent(string eventName);
    void TrackEvent(string eventName, Dictionary<string, object> props);
}

public class InitOptions
{
    /// <summary>
    /// Custom host for Self-Hosted instances
    /// </summary>
    public string? Host { get; set; }
}

/// <summary>
/// Aptabase client used for tracking events
/// </summary>
public class AptabaseClient : IAptabaseClient
{
    private static readonly Random _random = new();
    private static readonly TimeSpan SESSION_TIMEOUT = TimeSpan.FromMinutes(60);

    private readonly HttpClient? _http;
    private DateTime _lastTouched = DateTime.UtcNow;
    private string _sessionId = NewSessionId();

    private static readonly Dictionary<string, string> _hosts =
        new()
        {
            { "US", "https://us.aptabase.com" },
            { "EU", "https://eu.aptabase.com" },
            { "DEV", "http://localhost:3000" },
            { "SH", "" },
        };

    /// <summary>
    /// Initializes a new Aptabase Client
    /// </summary>
    /// <param name="appKey">The App Key.</param>
    /// <param name="options">Initialization Options.</param>
    public AptabaseClient(string appKey, InitOptions? options)
    {
        var parts = appKey.Split("-");
        if (parts.Length != 3 || !_hosts.ContainsKey(parts[1]))
        {
            throw new ArgumentException("Invalid App Key");
        }

        var baseUrl = GetBaseUrl(parts[1], options);
        if (baseUrl is null)
            return;

        _http = new() { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Add("App-Key", appKey);
    }

    private static string? GetBaseUrl(string region, InitOptions? options)
    {
        if (region == "SH")
        {
            if (string.IsNullOrEmpty(options?.Host))
            {
                throw new ArgumentException("Host is required for Self-Hosted instances");
            }

            return options.Host;
        }

        return _hosts[region];
    }

    /// <summary>
    /// Sends a telemetry event to Aptabase
    /// </summary>
    /// <param name="eventName">The event name.</param>
    public void TrackEvent(string eventName)
    {
        this.TrackEvent(eventName, null);
    }

    /// <summary>
    /// Sends a telemetry event to Aptabase
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="props">A list of key/value pairs.</param>
    public void TrackEvent(string eventName, Dictionary<string, object>? props)
    {
        Task.Run(() => SendEvent(eventName, props));
    }

    private async Task SendEvent(string eventName, Dictionary<string, object>? props)
    {
        if (_http is null)
            return;

        try
        {
            var now = DateTime.UtcNow;
            var timeSince = now.Subtract(_lastTouched);
            if (timeSince >= SESSION_TIMEOUT)
                _sessionId = NewSessionId();

            _lastTouched = now;

            var body = JsonContent.Create(
                new
                {
                    timestamp = DateTime.UtcNow.ToString("o"),
                    sessionId = _sessionId,
                    eventName,
                    systemProps = new
                    {
                        isDebug = SystemInfo.IsInDebugMode(),
                        osName = SystemInfo.GetOsName(),
                        osVersion = SystemInfo.GetOsVersion(),
                        locale = SystemInfo.GetLocale(),
                        appVersion = InstallationInfo.GetVersionString(),
                        sdkVersion = "0.0.7"
                    },
                    props
                }
            );

            var response = await _http.PostAsync("/api/v0/event", body);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                Log.Logger.Error("Aptabase Error: {0}", responseBody);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Aptabase Error: Failed to send event {0}", ex.Message);
        }
    }

    public static string NewSessionId()
    {
        var epochInSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = _random.NextInt64(0, 99999999);
        return (epochInSeconds * 100000000 + random).ToString();
    }
}
