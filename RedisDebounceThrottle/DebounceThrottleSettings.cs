using System;

namespace RedisDebounceThrottle
{
    /// <summary>
    /// Represents settings for configuring debounce and throttle mechanisms, especially for use with Redis.
    /// This includes settings for key naming conventions and lock expiry times.
    /// </summary>
    public class DebounceThrottleSettings
    {
        /// <summary>
        /// Gets or sets the prefix added to all Redis keys used by the debounce and throttle services.
        /// This helps in namespace isolation and avoiding key collisions in Redis.
        /// Default value is "debounce-throttle:", which ensures that the keys are easily identifiable
        /// and grouped together in Redis.
        /// </summary>
        public string RedisKeysPrefix { get; set; } = "debounce-throttle:";
        
        /// <summary>
        /// Gets or sets the expiry time for distributed locks (RedLocks) used to ensure atomic operations
        /// across distributed instances. This setting is crucial for preventing deadlocks and ensuring
        /// that locks are released in case of process failures or network partitions.
        /// Default value is 10 seconds, providing a balance between lock reliability and responsiveness
        /// in distributed environments.
        /// </summary>
        public TimeSpan RedLockExpiryTime { get; set; } = TimeSpan.FromSeconds(10);
    }
}
