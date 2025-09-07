using System.Text.Json;
using StackExchange.Redis;

namespace RemoteCollections.Redis.Serializing;

public class JsonRedisSerializer : IRedisSerializer
{
    public RedisValue Serialize<T>(T? value) => JsonSerializer.Serialize(value);

    public T Deserialize<T>(RedisValue value) => JsonSerializer.Deserialize<T>(value.ToString());
}