using System.Net;
using System.Security.Cryptography;

namespace Guard.Core.Token
{
    // Steam decided to ignore the standard and created a custom TOTP algorithm
    // Inspired by https://github.com/hmlendea/steamguard.totp and https://github.com/kspearrin/Otp.NET
    public class SteamTokenGenerator
    {
        /// <summary>
        /// The number of ticks as Measured at Midnight Jan 1st 1970;
        /// </summary>
        private const long UnicEpocTicks = 621355968000000000L;

        /// <summary>
        /// A divisor for converting ticks to seconds
        /// </summary>
        private const long TicksToSeconds = 10000000L;

        /// <summary>
        /// Steam tokens are valid for 30 seconds
        /// </summary>
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        private static readonly int CodeLength = 5;
        private static readonly string Alphabet = "23456789BCDFGHJKMNPQRTVWXY";

        public static string ComputeTotp(byte[] secret)
        {
            using HMACSHA1 hasher = new(secret);

            long currentTimeStep = GetCurrentTimeStep();
            var bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((long)currentTimeStep));
            var hmac = hasher.ComputeHash(bytes);
            byte offset = (byte)(hmac[19] & 15);

            int binCode =
                ((hmac[offset] & 127) << 24)
                | ((hmac[offset + 1] & 255) << 16)
                | ((hmac[offset + 2] & 255) << 8)
                | (hmac[offset + 3] & 255);

            string code = string.Empty;

            for (int i = 0; i < CodeLength; ++i)
            {
                code += Alphabet[binCode % Alphabet.Length];
                binCode /= Alphabet.Length;
            }

            return code;
        }

        private static long GetCurrentTimeStep()
        {
            long unixTimestamp = (DateTime.UtcNow.Ticks - UnicEpocTicks) / TicksToSeconds;
            return unixTimestamp / (long)30;
        }
    }
}
