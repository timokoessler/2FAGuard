namespace Guard.Core.CliBridge
{
    public class CliBridgeRateLimiter
    {
        private readonly int maxRequests;
        private readonly TimeSpan window;
        private readonly Func<DateTime> getNow;
        private readonly List<DateTime> globalRequestTimes = [];
        private readonly Dictionary<int, List<DateTime>> requestTimes = [];
        private readonly object lockObject = new();

        public CliBridgeRateLimiter(int maxRequests, TimeSpan window, Func<DateTime>? getNow = null)
        {
            this.maxRequests = maxRequests;
            this.window = window;
            this.getNow = getNow ?? (() => DateTime.UtcNow);
        }

        public bool Allow(int processId)
        {
            lock (lockObject)
            {
                DateTime now = getNow();
                globalRequestTimes.RemoveAll(time => now - time > window);
                if (globalRequestTimes.Count >= maxRequests)
                {
                    return false;
                }

                if (!requestTimes.TryGetValue(processId, out List<DateTime>? times))
                {
                    times = [];
                    requestTimes[processId] = times;
                }

                times.RemoveAll(time => now - time > window);
                if (times.Count >= maxRequests)
                {
                    return false;
                }

                globalRequestTimes.Add(now);
                times.Add(now);
                return true;
            }
        }
    }
}
