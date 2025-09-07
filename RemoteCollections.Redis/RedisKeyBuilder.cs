namespace RemoteCollections.Redis;

internal static class RedisKeyBuilder
{
    public static string GetRedisKey(string typeName, string keySuffix) => $"{typeName}+{keySuffix}";
}