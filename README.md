# DistributedDebounceThrottle

`DistributedDebounceThrottle` is a .NET library designed to facilitate debounce and throttle mechanisms in distributed system environments, leveraging Redis for state management and distributed locking. This ensures that function executions are properly debounced or throttled across multiple instances, preventing excessive or unintended operations.

## Features

- **Debounce**: Ensures a function is executed only once after a specified interval since the last call, useful for minimizing redundant operations over a period.
- **Throttle**: Limits the execution frequency of a function, ensuring it's not executed more than once within a specified timeframe.
- **Distributed Locks**: Implements the RedLock algorithm for distributed locking to coordinate debounce and throttle logic across distributed systems.
- **Redis Integration**: Utilizes Redis for managing timestamps and locks, offering a scalable solution for state synchronization.

## Getting Started

### Installation

Install `DistributedDebounceThrottle` via NuGet:

```shell
dotnet add package DistributedDebounceThrottle
```

## Usage
To integrate `DistributedDebounceThrottle` in your application, ensure you have a Redis instance ready for connection. Here's how to get started:

### 1. Configure Services:

In your application's startup configuration, register DistributedDebounceThrottle:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Using an existing IConnectionMultiplexer instance:
    services.AddDistributedDebounceThrottle(settings);

    // Or, initiating a new IConnectionMultiplexer with a connection string:
    services.AddDistributedDebounceThrottle(redisConnectionString, settings);
}
```

### 2. Inject and Use `IDebounceThrottle`:

Inject `IDebounceThrottle` to access debounce and throttle dispatchers:
```csharp
public class SampleService
{
    private readonly IDispatcher _throttler;

    public SampleService(IDebounceThrottle debounceThrottle)
    {
        _throttler = debounceThrottle.ThrottleDispatcher("uniqueDispatcherId", TimeSpan.FromSeconds(5));
    }

    public Task ExecuteThrottledOperation()
    {
        return _throttler.DispatchAsync(async () =>
        {
            // Operation logic here.
        });
    }
}
```

## Configuration
Customize your debounce and throttle settings via `DebounceThrottleSettings`:

- `RedisKeysPrefix`: A prefix for all Redis keys (default "debounce-throttle:").
- `RedLockExpiryTime`: The expiry time for the distributed locks (default TimeSpan.FromSeconds(10)).

## Contributing
Contributions are welcome! Feel free to fork the repository, submit pull requests, or report issues for bugs, features, or documentation improvements.

## License
This project is licensed under the MIT License - see the LICENSE file in the repository for details.
