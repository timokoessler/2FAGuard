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
                Assert.True(limiter.Allow(123));
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
                Assert.True(limiter.Allow(123));
            }

            Assert.False(limiter.Allow(123));
        }

        [Fact]
        public void BlocksRequestsAcrossProcessIds()
        {
            DateTime now = new(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc);
            var limiter = new Guard.Core.CliBridge.CliBridgeRateLimiter(
                5,
                TimeSpan.FromSeconds(30),
                () => now
            );

            for (int i = 0; i < 5; i++)
            {
                Assert.True(limiter.Allow(100 + i));
            }

            Assert.False(limiter.Allow(200));
        }

        [Fact]
        public void GlobalLimitResetsAfterWindowExpires()
        {
            DateTime now = new(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc);
            var limiter = new Guard.Core.CliBridge.CliBridgeRateLimiter(
                5,
                TimeSpan.FromSeconds(30),
                () => now
            );

            for (int i = 0; i < 5; i++)
            {
                Assert.True(limiter.Allow(100 + i));
            }

            now = now.AddSeconds(31);

            Assert.True(limiter.Allow(200));
        }

        [Fact]
        public void AllowsRequestsAfterWindowExpires()
        {
            DateTime now = new(2026, 5, 6, 12, 0, 0, DateTimeKind.Utc);
            var limiter = new Guard.Core.CliBridge.CliBridgeRateLimiter(
                5,
                TimeSpan.FromSeconds(30),
                () => now
            );

            for (int i = 0; i < 5; i++)
            {
                Assert.True(limiter.Allow(123));
            }

            now = now.AddSeconds(31);

            Assert.True(limiter.Allow(123));
        }
    }
}
