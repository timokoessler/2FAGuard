using Guard.Core.CliBridge;

namespace Guard.Test.Core
{
    public class CliBridgeRateLimiter
    {
        [Fact]
        public void AllowsRequestsWithinLimit()
        {
            DateTime now = new(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc);
            var limiter = new Guard.Core.CliBridge.CliBridgeRateLimiter(
                5,
                TimeSpan.FromSeconds(30),
                () => now
            );

            for (int i = 0; i < 5; i++)
            {
                Assert.True(limiter.Allow());
            }
        }

        [Fact]
        public void BlocksRequestsAfterLimit()
        {
            DateTime now = new(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc);
            var limiter = new Guard.Core.CliBridge.CliBridgeRateLimiter(
                5,
                TimeSpan.FromSeconds(30),
                () => now
            );

            for (int i = 0; i < 5; i++)
            {
                Assert.True(limiter.Allow());
            }

            Assert.False(limiter.Allow());
        }

        [Fact]
        public void ZeroMaxRequestsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Guard.Core.CliBridge.CliBridgeRateLimiter(0, TimeSpan.FromSeconds(30))
            );
        }

        [Fact]
        public void NegativeMaxRequestsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Guard.Core.CliBridge.CliBridgeRateLimiter(-1, TimeSpan.FromSeconds(30))
            );
        }

        [Fact]
        public void ZeroWindowThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Guard.Core.CliBridge.CliBridgeRateLimiter(5, TimeSpan.Zero)
            );
        }

        [Fact]
        public void NegativeWindowThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new Guard.Core.CliBridge.CliBridgeRateLimiter(5, TimeSpan.FromSeconds(-1))
            );
        }

        [Fact]
        public void LimitResetsAfterWindowExpires()
        {
            DateTime now = new(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc);
            var limiter = new Guard.Core.CliBridge.CliBridgeRateLimiter(
                5,
                TimeSpan.FromSeconds(30),
                () => now
            );

            for (int i = 0; i < 5; i++)
            {
                Assert.True(limiter.Allow());
            }

            now = now.AddSeconds(31);

            Assert.True(limiter.Allow());
        }
    }
}
