using System.Net.Http;

namespace Guard.WPF.Core
{
    internal class HTTP
    {
        public static readonly HttpClient httpClient = new();

        static HTTP()
        {
            httpClient.DefaultRequestHeaders.Add("User-Agent", "2FAGuard-Windows-App");
        }

        public static HttpClient GetHttpClient()
        {
            return httpClient;
        }
    }
}
