using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Channels;
using Guard.Core;

namespace Guard.WPF.Core.Aptabase;

/*
 * MIT License
 * Copyright (c) 2023 Sumbit Labs Ltd.
 * https://github.com/aptabase/aptabase-maui
 * Some modifications were made to the original work to fit the context of this project and make it work without MAUI.
 * */

/// <summary>
/// Aptabase client used for tracking events
/// </summary>
public interface IAptabaseClient
{
    void TrackEvent(string eventName, Dictionary<string, object>? props = null);
}

/// <summary>
/// Initialization options for the Aptabase Client
/// </summary>
public class AptabaseOptions
{
    /// <summary>
    /// Specifies the custom host URL for Self-Hosted instances of the Aptabase. This setting is required for Self-Hosted instances.
    /// </summary>
    public string? Host { get; set; }
}

/// <summary>
/// Aptabase client used for tracking events
/// </summary>
public class AptabaseClient : IAptabaseClient, IAsyncDisposable
{
    internal class EventData(string eventName, Dictionary<string, object>? props = null)
    {
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
        public string EventName { get; set; } = eventName;
        public Dictionary<string, object>? Props { get; set; } = props;
        public AptabaseSystemInfo? SystemProps { get; set; }
        public string? SessionId { get; set; }
    }

    internal class AptabaseSystemInfo
    {
        public bool IsDebug { get; set; }
        public string? OsName { get; set; }
        public string? OsVersion { get; set; }
        public string? DeviceModel { get; set; }
        public string? SdkVersion { get; set; }
        public string? Locale { get; set; }
        public string? AppVersion { get; set; }
        public string? AppBuildNumber { get; set; }
    }

    private static readonly Random _random = new();
    private static readonly TimeSpan SESSION_TIMEOUT = TimeSpan.FromMinutes(60);

    private readonly HttpClient? _http;
    private DateTime _lastTouched = DateTime.UtcNow;
    private string _sessionId = NewSessionId();

    private readonly Channel<EventData>? _channel;
    private readonly Task? _processingTask;

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
    public AptabaseClient(string appKey, AptabaseOptions? options)
    {
        var parts = appKey.Split("-");
        if (parts.Length != 3 || !_hosts.ContainsKey(parts[1]))
        {
            Log.Logger.Error("Aptabase: Invalid App Key");
            return;
        }

        var baseUrl = GetBaseUrl(parts[1], options);
        if (baseUrl is null)
        {
            Log.Logger.Warning("Aptabase: No base URL provided");
            return;
        }

        _http = new() { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Add("App-Key", appKey);

        _channel = Channel.CreateUnbounded<EventData>();

        _processingTask = Task.Run(ProcessEventsAsync);
    }

    /// <summary>
    /// Sends a telemetry event to Aptabase
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="props">A list of key/value pairs.</param>
    public void TrackEvent(string eventName, Dictionary<string, object>? props = null)
    {
        if (_channel is null)
        {
            return;
        }

        if (!_channel.Writer.TryWrite(new EventData(eventName, props)))
        {
            Log.Logger.Warning("Aptabase: Failed to perform TrackEvent");
        }
    }

    private async ValueTask ProcessEventsAsync()
    {
        if (_channel is null)
        {
            return;
        }

        while (await _channel.Reader.WaitToReadAsync())
        {
            RefreshSession();

            while (_channel.Reader.TryRead(out EventData? eventData))
            {
                await SendEventAsync(eventData);
            }
        }
    }

    private async Task SendEventAsync(EventData? eventData)
    {
        if (eventData is null)
        {
            return;
        }

        if (_http is null)
        {
            return;
        }

        try
        {
            eventData.SessionId = _sessionId;
            eventData.SystemProps = new AptabaseSystemInfo()
            {
                IsDebug = SystemInfo.IsInDebugMode(),
                OsName = SystemInfo.GetOsName(),
                OsVersion = SystemInfo.GetOsVersion(),
                Locale = SystemInfo.GetLocale(),
                AppVersion = InstallationContext.GetVersionString(),
                AppBuildNumber = "",
                SdkVersion = "0.0.9",
                DeviceModel = ""
            };

            var body = JsonContent.Create(eventData);

            var response = await _http.PostAsync("/api/v0/event", body);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();

                Log.Logger.Warning(
                    "Aptabase: Failed to perform TrackEvent due to {StatusCode} and response body {Body}",
                    response.StatusCode,
                    responseBody
                );
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(
                "Aptabase: Failed to perform TrackEvent due to {Exception}",
                ex.Message
            );
        }
    }

    private void RefreshSession()
    {
        var now = DateTime.UtcNow;
        var timeSince = now.Subtract(_lastTouched);

        if (timeSince >= SESSION_TIMEOUT)
        {
            _sessionId = NewSessionId();
        }

        _lastTouched = now;
    }

    private static string NewSessionId()
    {
        var epochInSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = _random.NextInt64(0, 99999999);

        return (epochInSeconds * 100000000 + random).ToString();
    }

    private static string? GetBaseUrl(string region, AptabaseOptions? options)
    {
        if (region == "SH")
        {
            if (string.IsNullOrEmpty(options?.Host))
            {
                Log.Logger.Warning(
                    "Aptabase: Host parameter must be defined when using Self-Hosted App Key. Tracking will be disabled."
                );
                return null;
            }

            return options.Host;
        }

        return _hosts[region];
    }

    public async ValueTask DisposeAsync()
    {
        _channel?.Writer.Complete();

        if (_processingTask?.IsCompleted == false)
        {
            await _processingTask;
        }

        _http?.Dispose();

        GC.SuppressFinalize(this);
    }
}
