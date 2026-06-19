using MediSphere.Application.Interfaces;

namespace MediSphere.Infrastructure.Services;

public class NoOpCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key)
    {
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null)
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(false);
    }
}