using System.Threading.Tasks;
using System;
using StackExchange.Redis;
using RedLockNet;

namespace RedisDebounceThrottle
{
    /// <summary>
    /// A dispatcher that throttles execution of functions to ensure that they are not executed
    /// more frequently than a specified interval. This implementation is suited for distributed environments
    /// where actions from multiple instances need to be coordinated to prevent excessive frequency.
    /// </summary>
    internal class ThrottleDispatcher : IDispatcher
    {
        private readonly string dispatcherId;
        private readonly TimeSpan interval;
        private readonly IDatabase database;
        private readonly IDistributedLockFactory lockFactory;
        private readonly DebounceThrottleSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottleDispatcher"/> class.
        /// </summary>
        /// <param name="dispatcherId">A unique identifier for the dispatcher to differentiate it from others.</param>
        /// <param name="interval">The minimum time interval between allowed executions.</param>
        /// <param name="database">The database connection used for storing the timestamp of the last execution.</param>
        /// <param name="lockFactory">The factory for creating distributed locks.</param>
        /// <param name="settings">Settings for throttle behavior, including keys' prefix and lock expiry time.</param>
        internal ThrottleDispatcher(
            string dispatcherId,
            TimeSpan interval,
            IDatabase database,
            IDistributedLockFactory lockFactory,
            DebounceThrottleSettings settings = null)
        {
            this.dispatcherId = dispatcherId;
            this.interval = interval;
            this.database = database;
            this.lockFactory = lockFactory;
            this.settings = settings ?? new DebounceThrottleSettings();
        }

        // Redis key patterns for storing the the time of the last execution, and the lock key.
        private string TimeKey => $"{settings.RedisKeysPrefix}throttle:{dispatcherId}:time";
        private string LockKey => $"{settings.RedisKeysPrefix}throttle:{dispatcherId}:lock";

        /// <summary>
        /// Attempts to dispatch the given function for execution if the specified interval has passed since the last execution.
        /// This method acquires a distributed lock to ensure that only one instance can make this check and possibly execute the function at a time.
        /// </summary>
        /// <param name="function">The function to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DispatchAsync(Func<Task> function)
        {
            using (IRedLock redlock = lockFactory.CreateLock(LockKey, expiryTime: settings.RedLockExpiryTime))
            {                
                if (!redlock.IsAcquired)
                {
                    return; // Exit if the lock is not acquired, indicating another instance might be executing the function.
                }

                long currentTicks = DateTimeOffset.UtcNow.Ticks;
                string cachedInvokeTimeStr = await database.StringGetAsync(TimeKey);

                // Check if the previous execution timestamp exists and if the current time is within the specified interval.
                bool valid = long.TryParse(cachedInvokeTimeStr, out long cachedInvokeTime);
                if (valid && TimeSpan.FromTicks(currentTicks - cachedInvokeTime).Ticks < interval.Ticks)
                {
                    return; // Exit if the interval has not yet passed since the last execution.
                }

                // Update the time of the last execution.
                await database.StringSetAsync(TimeKey, currentTicks.ToString());

                // Execute the function.
                await function.Invoke();
            }
        }
    }
}
