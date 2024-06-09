using GuerrillaNtp;
using Guard.Core;

namespace Guard.WPF.Core
{
    internal class Time
    {
        public static async Task<TimeSpan> GetLocalUTCTimeOffset()
        {
            try
            {
                NtpClient client = NtpClient.Default;
                NtpClock clock = await client.QueryAsync();

                return clock.UtcNow - DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(
                    "Failed to get UTC time offset from NTP server: {0}",
                    ex.Message
                );
                return TimeSpan.Zero;
            }
        }
    }
}
