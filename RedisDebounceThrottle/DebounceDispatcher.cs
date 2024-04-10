using RedLockNet;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace RedisDebounceThrottle
{
    /// <summary>
    /// A dispatcher that debounces execution of functions in a distributed environment.
    /// This ensures that functions are not executed too frequently within a specified interval, 
    /// and optionally, that they are executed at least once within a maximum delay period.
    /// </summary>
    internal class DebounceDispatcher : IDispatcher
    {
        private readonly string dispatcherId;
        private readonly TimeSpan interval;
        private readonly TimeSpan? maxDelay;
        private readonly IDatabase database;
        private readonly IDistributedLockFactory lockFactory;
        private readonly DebounceThrottleSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebounceDispatcher"/> class.
        /// </summary>
        /// <param name="dispatcherId">A unique identifier for the dispatcher.</param>
        /// <param name="interval">The minimum time interval between successive executions.</param>
        /// <param name="maxDelay">The maximum delay for an execution since the first trigger, after which the action must be executed. Can be null.</param>
        /// <param name="database">Redis database connection used for storing timestamps.</param>
        /// <param name="lockFactory">The factory for creating RedLock.</param>
        /// <param name="settings">Settings for debounce.</param>
        internal DebounceDispatcher(
            string dispatcherId,
            TimeSpan interval,
            TimeSpan? maxDelay,
            IDatabase database,
            IDistributedLockFactory lockFactory,
            DebounceThrottleSettings settings = null)
        {
            this.dispatcherId = dispatcherId;
            this.interval = interval;
            this.maxDelay = maxDelay;
            this.database = database;
            this.lockFactory = lockFactory;
            this.settings = settings ?? new DebounceThrottleSettings();
        }

        // Redis key patterns for storing initialization time, the time of the last attempt, and the lock key.
        private string InitKey => $"{settings.RedisKeysPrefix}debounce:{dispatcherId}:init";
        private string TimeKey => $"{settings.RedisKeysPrefix}debounce:{dispatcherId}:time";
        private string LockKey => $"{settings.RedisKeysPrefix}debounce:{dispatcherId}:lock";

        /// <summary>
        /// Dispatches a function for execution, applying debounce logic.
        /// </summary>
        /// <param name="function">The function to be executed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DispatchAsync(Func<Task> function)
        {            
            using (IRedLock redlock = lockFactory.CreateLock(LockKey, expiryTime: settings.RedLockExpiryTime))
            {
                if (!redlock.IsAcquired)
                {
                    return; // Exit if unable to acquire lock, ensuring only one execution at a time.
                }

                // Update the time key with current time to mark the latest attempt.
                long currentTicks = DateTime.UtcNow.Ticks;
                await database.StringSetAsync(TimeKey, currentTicks.ToString());

                // Retrieve and parse the initialization time. If not set, initialize it.
                string cachedInitInvokeTimeStr = await database.StringGetAsync(InitKey);
                if (!long.TryParse(cachedInitInvokeTimeStr, out long initTime))
                {
                    initTime = currentTicks;
                    await database.StringSetAsync(InitKey, initTime.ToString());
                }

                // Calculate the time difference from the initial trigger.
                double initTimeDiff = TimeSpan.FromTicks(currentTicks - initTime).Ticks;

                // Check if execution is due by maxDelay or proceed with regular interval checking.
                if (maxDelay.HasValue && initTimeDiff > maxDelay.Value.Ticks)
                {
                    await InvokeAsync(function); // Time exceeded, invoke immediately.
                }
                else
                {
                    // Calculate remaining time to the nearest execution window.
                    long leftToMax = (long) (maxDelay.HasValue ? maxDelay.Value.Ticks - initTimeDiff : long.MaxValue);
                    long delay = Math.Min(leftToMax, interval.Ticks);

                    _ = Task.Run(() => AttemptionAsync(function, delay)); // Schedule delayed execution attempt.
                }
            }
        }

        /// <summary>
        /// Invokes the function and cleans up resources.
        /// </summary>
        /// <param name="function">The function to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task InvokeAsync(Func<Task> function)
        {
            try
            {
                await function.Invoke(); // Execute the function.
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                // Always clean up after execution to reset the debounce.
                await database.KeyDeleteAsync(TimeKey);
                await database.KeyDeleteAsync(InitKey);
            }
        }

        /// <summary>
        /// Attempts to invoke the function after a specified delay, ensuring conditions are still met.
        /// </summary>
        /// <param name="function">The function to execute.</param>
        /// <param name="delay">The delay in ticks before attempting execution.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task AttemptionAsync(Func<Task> function, long delay)
        {
            // Wait for the specified delay before attempting to execute.
            await Task.Delay((int) TimeSpan.FromTicks(delay).TotalMilliseconds);

            using (IRedLock redlock = lockFactory.CreateLock(LockKey, expiryTime: settings.RedLockExpiryTime))
            {
                if (!redlock.IsAcquired)
                {                 
                    return; // Exit if unable to re-acquire lock, ensuring only one execution at a time.
                }

                // Check the time since the last invocation attempt.
                string cachedInvokeTimeStr = await database.StringGetAsync(TimeKey);
                bool timeValid = long.TryParse(cachedInvokeTimeStr, out long invokeTime);

                long currentTicks = DateTime.UtcNow.Ticks;
                double invokeTimeDiff = TimeSpan.FromTicks(currentTicks - invokeTime).Ticks;

                // If the interval has been met, proceed with invocation.
                if (timeValid && invokeTimeDiff >= interval.Ticks)
                {
                    await InvokeAsync(function);
                }                
            }
        }
    }
}
