namespace Redis.Collections.Generic;

internal static class RedisKeyBuilder
{
    public static string GetRedisKey(object obj, string keySuffix) => $"{obj}({keySuffix})";
}