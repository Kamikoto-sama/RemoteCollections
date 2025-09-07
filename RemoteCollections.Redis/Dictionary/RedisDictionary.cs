using System.Collections;
using System.Diagnostics.CodeAnalysis;
using RemoteCollections.Redis.Serializing;
using StackExchange.Redis;

namespace RemoteCollections.Redis.Dictionary;

public class RedisDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
{
    private readonly IDatabase database;
    private readonly RedisKey redisKey;
    private readonly IRedisSerializer keySerializer;
    private readonly IRedisSerializer valueSerializer;

    public int Count => (int)database.HashLength(redisKey);
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys =>
        database.HashKeys(redisKey).Select(k => keySerializer.Deserialize<TKey>(k)).ToList();

    public ICollection<TValue> Values =>
        database.HashValues(redisKey).Select(v => valueSerializer.Deserialize<TValue>(v)).ToList();

    public TValue this[TKey key]
    {
        get
        {
            if (TryGetValue(key, out var value))
                return value;
            throw new KeyNotFoundException($"Key '{key}' not found in dictionary");
        }
        set => Set(key, value, true);
    }

    public RedisDictionary(IDatabase database, string name, RedisCollectionOptions options)
    {
        this.database = database;
        keySerializer = options.KeySerializer;
        valueSerializer = options.ValueSerializer;
        redisKey = RedisKeyBuilder.GetRedisKey(nameof(IDictionary<TKey, TValue>), name);
    }

    public void Add(TKey key, TValue value)
    {
        if (Set(key, value, false))
            return;

        throw new ArgumentException("An item with the same key has already been added");
    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var hashKey = keySerializer.Serialize(key);
        var hashValue = database.HashGet(redisKey, hashKey);

        value = default;
        if (hashValue.IsNullOrEmpty)
            return false;
        value = valueSerializer.Deserialize<TValue>(hashValue);
        return true;
    }

    public void Clear() => database.KeyDelete(redisKey);

    public bool ContainsKey(TKey key) => database.HashExists(redisKey, keySerializer.Serialize(key));

    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);

    public bool Remove(TKey key)
    {
        var hashKey = keySerializer.Serialize(key);
        return database.HashDelete(redisKey, hashKey);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        var entries = database.HashGetAll(redisKey);
        foreach (var hashEntry in entries) 
            array[arrayIndex++] = ToKeyValuePair(hashEntry);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => 
        database.HashScan(redisKey).Select(ToKeyValuePair).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private bool Set(TKey key, TValue value, bool replace)
    {
        var hashKey = keySerializer.Serialize(key);
        var hashValue = valueSerializer.Serialize(value);
        var when = replace ? When.Always : When.NotExists;
        return database.HashSet(redisKey, hashKey, hashValue, when);
    }

    private KeyValuePair<TKey, TValue> ToKeyValuePair(HashEntry hashEntry)
    {
        var key = keySerializer.Deserialize<TKey>(hashEntry.Name);
        var value = valueSerializer.Deserialize<TValue>(hashEntry.Value);
        return KeyValuePair.Create(key, value);
    }
}