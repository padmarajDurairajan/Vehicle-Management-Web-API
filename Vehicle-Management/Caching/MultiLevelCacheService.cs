using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace VehicleManagementApi.Caching;

public class MultiLevelCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<MultiLevelCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MultiLevelCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<MultiLevelCacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan memoryExpiration,
        TimeSpan distributedExpiration,
        CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(key, out T? memoryValue) && memoryValue is not null)
        {
            _logger.LogInformation("CACHE HIT - Memory - Key={Key}", key);
            return memoryValue;
        }

        _logger.LogInformation("CACHE MISS - Memory - Key={Key}", key);

        var distributedJson = await _distributedCache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrWhiteSpace(distributedJson))
        {
            var distributedValue = JsonSerializer.Deserialize<T>(distributedJson, _jsonOptions);

            if (distributedValue is not null)
            {
                _memoryCache.Set(
                    key,
                    distributedValue,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = memoryExpiration
                    });

                _logger.LogInformation("CACHE HIT - Redis - Key={Key}", key);
                return distributedValue;
            }
        }

        _logger.LogInformation("CACHE MISS - Redis - Key={Key}", key);

        var value = await factory(cancellationToken);
        if (value is null)
            return default;

        _memoryCache.Set(
            key,
            value,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = memoryExpiration
            });

        await _distributedCache.SetStringAsync(
            key,
            JsonSerializer.Serialize(value, _jsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = distributedExpiration
            },
            cancellationToken);

        _logger.LogInformation("CACHE SET - Memory + Redis - Key={Key}", key);

        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        await _distributedCache.RemoveAsync(key, cancellationToken);

        _logger.LogInformation("CACHE REMOVE - Memory + Redis - Key={Key}", key);
    }
}