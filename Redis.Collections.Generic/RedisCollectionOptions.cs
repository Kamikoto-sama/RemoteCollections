using Redis.Collections.Generic.Serializing;
using StackExchange.Redis;

namespace Redis.Collections.Generic;

public class RedisCollectionOptions
{
    public IRedisSerializer KeySerializer { get; set; } = new JsonRedisSerializer();
    public IRedisSerializer ValueSerializer { get; set; } =  new JsonRedisSerializer();
}