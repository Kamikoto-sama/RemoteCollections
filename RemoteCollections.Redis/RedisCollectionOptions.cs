using RemoteCollections.Redis.Serializing;

namespace RemoteCollections.Redis;

public class RedisCollectionOptions
{
    public IRedisSerializer KeySerializer { get; set; } = new JsonRedisSerializer();
    public IRedisSerializer ValueSerializer { get; set; } =  new JsonRedisSerializer();
}