using Microsoft.Extensions.DependencyInjection;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace RedisDebounceThrottle
{
    /// <summary>
    /// Contains extension methods for <see cref="IServiceCollection"/> to facilitate the registration
    /// of distributed debounce and throttle services using a Redis backend.
    /// </summary>
    public static class DebounceThrottleExtensions
    {
        /// <summary>
        /// Adds the <see cref="DebounceThrottle"/> service to the specified <see cref="IServiceCollection"/>,
        /// using a provided <see cref="IConnectionMultiplexer"/> from the service provider for Redis connections.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="settings">Optional. The settings to use for debounce and throttle operations. If not provided, default settings are used.</param>
        /// <returns>The original <see cref="IServiceCollection"/> instance, for chaining further calls.</returns>
        public static IServiceCollection AddDistributedDebounceThrottle(IServiceCollection services, DebounceThrottleSettings settings = null)
        {
            return services
                .AddTransient(sp =>
                {
                    IConnectionMultiplexer multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
                    return CreateDebounceThrottle(settings, multiplexer);
                });
        }

        /// <summary>
        /// Adds the <see cref="DebounceThrottle"/> service to the specified <see cref="IServiceCollection"/>,
        /// creating a new <see cref="IConnectionMultiplexer"/> instance using the provided Redis connection string.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="redisConnectionString">The Redis connection string used to connect to Redis.</param>
        /// <param name="settings">Optional. The settings to use for debounce and throttle operations. If not provided, default settings are used.</param>
        /// <returns>The original <see cref="IServiceCollection"/> instance, for chaining further calls.</returns>
        public static IServiceCollection AddDistributedDebounceThrottle(IServiceCollection services, string redisConenctionString, DebounceThrottleSettings settings = null)
        {
            return services
                .AddTransient(sp => 
                {
                    ConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect(redisConenctionString);
                    return CreateDebounceThrottle(settings, multiplexer);
                });            
        }

        /// <summary>
        /// Creates a <see cref="DebounceThrottle"/> instance using the provided settings and Redis connection.
        /// </summary>
        /// <param name="settings">The settings to use for the <see cref="DebounceThrottle"/> instance.</param>
        /// <param name="multiplexer">The Redis <see cref="IConnectionMultiplexer"/> for database and lock factory creation.</param>
        /// <returns>An instance of <see cref="IDebounceThrottle"/>.</returns>
        private static IDebounceThrottle CreateDebounceThrottle(DebounceThrottleSettings settings, IConnectionMultiplexer multiplexer)
        {
            IDatabase database = multiplexer.GetDatabase();
            RedLockMultiplexer redLockMultiplexer = new RedLockMultiplexer(multiplexer);
            RedLockFactory lockFactory = RedLockFactory.Create(new[] { redLockMultiplexer });

            return new DebounceThrottle(database, lockFactory, settings);
        }
    }
}
