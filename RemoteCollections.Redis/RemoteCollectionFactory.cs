using RemoteCollections.Redis.Dictionary;
using StackExchange.Redis;

namespace RemoteCollections.Redis;

public class RemoteCollectionFactory(IDatabase database)
{
    public RedisDictionary<TKey, TValue> CreateDict<TKey, TValue>(string name) where TKey : notnull
    {
        var options = new RedisCollectionOptions();
        return new RedisDictionary<TKey, TValue>(database, name, options);
    }
    
    public AsyncRedisDictionary<TKey, TValue> CreateDictAsync<TKey, TValue>(string name) where TKey : notnull
    {
        var options = new RedisCollectionOptions();
        return new AsyncRedisDictionary<TKey, TValue>(database, name, options);
    }
}