using GuerrillaNtp;

namespace Guard.Core
{
    internal class Time
    {
        public static async Task<TimeSpan> GetLocalUTCTimeOffset()
        {
            NtpClient client = NtpClient.Default;
            NtpClock clock = await client.QueryAsync();

            return clock.UtcNow - DateTimeOffset.UtcNow;
        }
    }
}
