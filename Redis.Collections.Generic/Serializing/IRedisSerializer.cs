using StackExchange.Redis;

namespace Redis.Collections.Generic.Serializing;

public interface IRedisSerializer
{
    RedisValue Serialize<T>(T value);
    T Deserialize<T>(RedisValue value);
}