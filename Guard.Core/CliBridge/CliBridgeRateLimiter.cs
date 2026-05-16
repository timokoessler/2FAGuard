namespace Guard.Core.CliBridge
{
    public class CliBridgeRateLimiter
    {
        private readonly int maxRequests;
        private readonly TimeSpan window;
        private readonly Func<DateTime> getNow;
        private readonly Queue<DateTime> requestTimes = [];
        private readonly object lockObject = new();

        public CliBridgeRateLimiter(int maxRequests, TimeSpan window, Func<DateTime>? getNow = null)
        {
            if (maxRequests <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRequests),
                    "maxRequests must be greater than zero."
                );
            }
            if (window <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(window),
                    "window must be greater than zero."
                );
            }

            this.maxRequests = maxRequests;
            this.window = window;
            this.getNow = getNow ?? (() => DateTime.UtcNow);
        }

        public bool Allow()
        {
            lock (lockObject)
            {
                DateTime now = getNow();
                while (requestTimes.Count > 0 && now - requestTimes.Peek() > window)
                {
                    requestTimes.Dequeue();
                }

                if (requestTimes.Count >= maxRequests)
                {
                    return false;
                }

                requestTimes.Enqueue(now);
                return true;
            }
        }
    }
}
