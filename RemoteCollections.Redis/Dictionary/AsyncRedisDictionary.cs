using RemoteCollections.Redis.Serializing;
using StackExchange.Redis;

namespace RemoteCollections.Redis.Dictionary;

public class AsyncRedisDictionary<TKey, TValue> where TKey : notnull
{
    private readonly IDatabase database;
    private readonly RedisKey redisKey;
    private readonly IRedisSerializer keySerializer;
    private readonly IRedisSerializer valueSerializer;

    public AsyncRedisDictionary(
        IDatabase database,
        string name,
        RedisCollectionOptions options
    )
    {
        this.database = database;
        redisKey = RedisKeyBuilder.GetRedisKey(nameof(IDictionary<TKey, TValue>), name);
        keySerializer = options.KeySerializer;
        valueSerializer = options.ValueSerializer;
    }

    public async Task AddAsync(TKey key, TValue value)
    {
        var hashKey = keySerializer.Serialize(key);
        var hashValue = valueSerializer.Serialize(value);
        if (await database.HashExistsAsync(redisKey, hashKey))
            throw new ArgumentException("An item with the same key has already been added.");

        await database.HashSetAsync(redisKey, hashKey, hashValue);
    }

    public async Task<bool> TryGetValueAsync(TKey key)
    {
        var redisValue = await database.HashGetAsync(redisKey, keySerializer.Serialize(key));
        return redisValue.HasValue;
    }

    public async Task<TValue> GetValueAsync(TKey key)
    {
        if (!await TryGetValueAsync(key))
            throw new KeyNotFoundException();

        var redisValue = await database.HashGetAsync(redisKey, keySerializer.Serialize(key));
        return valueSerializer.Deserialize<TValue>(redisValue);
    }

    public async Task ClearAsync() => await database.KeyDeleteAsync(redisKey);

    public async Task<bool> ContainsKeyAsync(TKey key) =>
        await database.HashExistsAsync(redisKey, keySerializer.Serialize(key));

    public async Task<bool> RemoveAsync(TKey key) =>
        await database.HashDeleteAsync(redisKey, keySerializer.Serialize(key));

    public async Task<int> GetCountAsync() => (int)await database.HashLengthAsync(redisKey);

    public async IAsyncEnumerable<KeyValuePair<TKey, TValue>> GetAllAsync()
    {
        var entries = await database.HashGetAllAsync(redisKey);
        foreach (var entry in entries)
        {
            yield return new KeyValuePair<TKey, TValue>(
                keySerializer.Deserialize<TKey>(entry.Name),
                valueSerializer.Deserialize<TValue>(entry.Value)
            );
        }
    }

    public async Task<ICollection<TKey>> GetKeysAsync()
    {
        var entries = await database.HashGetAllAsync(redisKey);
        return entries.Select(entry => keySerializer.Deserialize<TKey>(entry.Name)).ToList();
    }

    public async Task<ICollection<TValue>> GetValuesAsync()
    {
        var entries = await database.HashGetAllAsync(redisKey);
        return entries.Select(entry => valueSerializer.Deserialize<TValue>(entry.Value)).ToList();
    }
}