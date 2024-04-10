using RedLockNet;
using StackExchange.Redis;
using System;

namespace RedisDebounceThrottle
{
    /// <summary>
    /// Provides functionality to create instances of debouncers and throttlers that can be used to limit
    /// the frequency of execution for given functions in a distributed environment. This class acts as a factory, producing dispatcher instances
    /// tailored to specific debouncing or throttling needs.
    /// </summary>
    public class DebounceThrottle : IDebounceThrottle
    {
        private readonly IDatabase database;
        private readonly IDistributedLockFactory lockFactory;
        private readonly DebounceThrottleSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebounceThrottle"/> class with specified settings.
        /// </summary>
        /// <param name="database">Redis database used for persistence in debouncing and throttling operations.</param>
        /// <param name="lockFactory">The factory responsible for creating RedLocks, ensuring atomic operations.</param>
        /// <param name="settings">The settings to apply across all created dispatchers. If not provided, default settings are used.</param>
        public DebounceThrottle(IDatabase database,
            IDistributedLockFactory lockFactory,
            DebounceThrottleSettings settings = null)
        {
            this.database = database;
            this.lockFactory = lockFactory;
            this.settings = settings ?? new DebounceThrottleSettings();
        }

        /// <summary>
        /// Creates a new <see cref="ThrottleDispatcher"/> instance, configured for throttling function executions.
        /// </summary>
        /// <param name="dispatcherId">The unique identifier for the dispatcher.</param>
        /// <param name="interval">The minimum interval between function executions.</param>
        /// <returns>A dispatcher instance configured for throttling.</returns>
        public IDispatcher ThrottleDispatcher(string dispatcherId, TimeSpan interval)
        {
            return new ThrottleDispatcher(dispatcherId, interval, database, lockFactory, settings);
        }

        /// <summary>
        /// Creates a new <see cref="DebounceDispatcher"/> instance, configured for debouncing function executions.
        /// This can include a maximum delay after which the function will be executed regardless of subsequent triggers.
        /// </summary>
        /// <param name="dispatcherId">The unique identifier for the dispatcher.</param>
        /// <param name="interval">The debounce interval, during which subsequent calls are ignored.</param>
        /// <param name="maxDelay">Optional. The maximum delay after the first trigger, ensuring the function is executed at least once within this timeframe.</param>
        /// <returns>A dispatcher instance configured for debouncing.</returns>
        public IDispatcher DebounceDispatcher(string dispatcherId, TimeSpan interval, TimeSpan? maxDelay = null)
        {
            return new DebounceDispatcher(dispatcherId, interval, maxDelay, database, lockFactory, settings);
        }
    }
}
