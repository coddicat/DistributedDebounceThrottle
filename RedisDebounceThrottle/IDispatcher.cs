using System;
using System.Threading.Tasks;

namespace RedisDebounceThrottle
{
    public interface IDispatcher
    {
        Task DispatchAsync(Func<Task> function);
    }
}
