using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MediSphere.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MediSphere.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly IDatabase? _redisDb;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<RedisCacheService>? _logger;
    private readonly ConcurrentDictionary<string, byte> _memoryKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _useRedis = false;

    public RedisCacheService(IConfiguration config, IMemoryCache memoryCache, ILogger<RedisCacheService>? logger = null)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        var connectionString = config.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                var options = ConfigurationOptions.Parse(connectionString);
                options.AbortOnConnectFail = false;
                options.ConnectTimeout = 1500;
                options.ConnectRetry = 1;

                _redis = ConnectionMultiplexer.Connect(options);
                _redisDb = _redis.GetDatabase();
                _useRedis = _redis.IsConnected;

                if (_useRedis)
                {
              _logger?.LogInformation("Redis connected successfully.");
}
else
{
    _logger?.LogWarning(
        "Redis multiplexer created but Redis is not connected. Using MemoryCache fallback.");
}
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis connection failed on startup. Falling back gracefully to MemoryCache.");
                _useRedis = false;
            }
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        if (_useRedis && _redisDb != null)
        {
            try
            {
                var value = await _redisDb.StringGetAsync(key);
                if (value.HasValue)
                {
                    return JsonSerializer.Deserialize<T>(value!);
                }
                return default;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis GetAsync failed for key: {Key}. Falling back to MemoryCache.", key);
            }
        }

        _memoryCache.TryGetValue(key, out T? memVal);
        return memVal;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        if (value == null) return;

        var json = JsonSerializer.Serialize(value);

        if (_useRedis && _redisDb != null)
        {
            try
            {
                await _redisDb.StringSetAsync(key, json, expiry);
                return;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis SetAsync failed for key: {Key}. Falling back to MemoryCache.", key);
            }
        }

        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.SetAbsoluteExpiration(expiry.Value);
        }
        else
        {
            options.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
        }

        _memoryCache.Set(key, value, options);
        _memoryKeys.TryAdd(key, 0);
    }

    public async Task RemoveAsync(string key)
    {
        if (_useRedis && _redisDb != null)
        {
            try
            {
                await _redisDb.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis RemoveAsync failed for key: {Key}.", key);
            }
        }

        _memoryCache.Remove(key);
        _memoryKeys.TryRemove(key, out _);
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return;

        if (_useRedis && _redis != null && _redisDb != null)
        {
            try
            {
                var endpoints = _redis.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = _redis.GetServer(endpoint);
                    if (server.IsConnected)
                    {
                        var keys = server.Keys(pattern: $"{prefix}*").ToArray();
                        if (keys.Length > 0)
                        {
                            await _redisDb.KeyDeleteAsync(keys);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis RemoveByPrefixAsync failed for prefix: {Prefix}.", prefix);
            }
        }

        var matchingKeys = _memoryKeys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in matchingKeys)
        {
            _memoryCache.Remove(key);
            _memoryKeys.TryRemove(key, out _);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        if (_useRedis && _redisDb != null)
        {
            try
            {
                return await _redisDb.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Redis ExistsAsync failed for key: {Key}.", key);
            }
        }

        return _memoryCache.TryGetValue(key, out _);
    }
}