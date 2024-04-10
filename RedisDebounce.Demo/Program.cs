using RedisDebounceThrottle;
using RedLockNet.SERedis.Configuration;
using RedLockNet.SERedis;
using StackExchange.Redis;

ConnectionMultiplexer multiplexer = ConnectionMultiplexer.Connect("localhost:6379");
IDatabase database = multiplexer.GetDatabase();
RedLockMultiplexer redLockMultiplexer = new (multiplexer);
RedLockFactory lockFactory = RedLockFactory.Create(new[] { redLockMultiplexer });

IDebounceThrottle redisDebounceThrottle = new DebounceThrottle(database, lockFactory);

var debounceDispatcher = redisDebounceThrottle.DebounceDispatcher("test", TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1500));
string str = "";

while (true)
{
    var key = Console.ReadKey(true);

    //trigger when to stop and exit
    if (key.Key == ConsoleKey.Escape) break;

    str += key.KeyChar;
    
    await debounceDispatcher.DispatchAsync(() =>
    {
        Console.WriteLine($"{str} - {DateTime.UtcNow:hh:mm:ss.fff}");
        str = "";
        return Task.CompletedTask;
    });
}