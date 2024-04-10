using RedisDebounceThrottle;
using RedLockNet.SERedis.Configuration;
using RedLockNet.SERedis;
using StackExchange.Redis;

ConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect("localhost:6379");
IDatabase database = multiplexer.GetDatabase();
RedLockMultiplexer redLockMultiplexer = new (multiplexer);
RedLockFactory lockFactory = RedLockFactory.Create(new[] { redLockMultiplexer });

IDebounceThrottle redisDebounceThrottle = new DebounceThrottle(database, lockFactory);

var throttleDispatcher = redisDebounceThrottle.ThrottleDispatcher("test", TimeSpan.FromMilliseconds(500));

while (true)
{
    var key = Console.ReadKey(true);

    //trigger when to stop and exit
    if (key.Key == ConsoleKey.Escape) break;

    await throttleDispatcher.DispatchAsync(() =>
    {
        Console.Write(".");
        return Task.CompletedTask;
    });
}