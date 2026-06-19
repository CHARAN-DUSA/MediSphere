    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using MediSphere.Application.Interfaces;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using StackExchange.Redis;

    namespace MediSphere.Infrastructure.Services;

    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase? _redisDb;
        private readonly IMemoryCache _memoryCache;
        private readonly bool _useRedis = false;

        public RedisCacheService(IConfiguration config, IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            var connectionString = config.GetConnectionString("Redis");
            
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                try
                {
                    var redis = ConnectionMultiplexer.Connect(connectionString);
                    _redisDb = redis.GetDatabase();
                    _useRedis = true;
                }
                catch
                {
                    // Fallback gracefully to memory cache if Redis is down
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
                catch
                {
                    // Fallback on transient Redis connection drops
                }
            }

            _memoryCache.TryGetValue(key, out T? memVal);
            return memVal;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var json = JsonSerializer.Serialize(value);

            if (_useRedis && _redisDb != null)
            {
                try
                {
                    await _redisDb.StringSetAsync(key, json, expiry);
                    return;
                }
                catch
                {
                    // Fallback
                }
            }

            var options = new MemoryCacheEntryOptions();
            if (expiry.HasValue)
            {
                options.SetAbsoluteExpiration(expiry.Value);
            }
            else
            {
                options.SetAbsoluteExpiration(TimeSpan.FromMinutes(30)); // 30m default
            }
            _memoryCache.Set(key, value, options);
        }

        public async Task RemoveAsync(string key)
        {
            if (_useRedis && _redisDb != null)
            {
                try
                {
                    await _redisDb.KeyDeleteAsync(key);
                    return;
                }
                catch
                {
                    // Fallback
                }
            }

            _memoryCache.Remove(key);
        }

        public async Task<bool> ExistsAsync(string key)
        {
            if (_useRedis && _redisDb != null)
            {
                try
                {
                    return await _redisDb.KeyExistsAsync(key);
                }
                catch
                {
                    // Fallback
                }
            }

            return _memoryCache.TryGetValue(key, out _);
        }
    }
