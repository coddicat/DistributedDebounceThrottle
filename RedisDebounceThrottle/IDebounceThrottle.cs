using System;

namespace RedisDebounceThrottle
{
    public interface IDebounceThrottle
    {
        IDispatcher ThrottleDispatcher(string dispatcherId, TimeSpan interval);
        IDispatcher DebounceDispatcher(string dispatcherId, TimeSpan interval, TimeSpan? maxDelay = null);
    }
}
