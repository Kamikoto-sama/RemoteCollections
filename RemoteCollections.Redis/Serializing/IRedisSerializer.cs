using StackExchange.Redis;

namespace RemoteCollections.Redis.Serializing;

public interface IRedisSerializer
{
    RedisValue Serialize<T>(T value);
    T Deserialize<T>(RedisValue value);
}