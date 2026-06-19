using MediSphere.Application.Interfaces;

namespace MediSphere.API.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var cache = context.RequestServices.GetService<ICacheService>();
        if (cache != null)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var key = $"rate-limit:{ip}:{DateTime.UtcNow:yyyyMMddHHmm}";
            try
            {
                var count = await cache.GetAsync<int>(key);
                if (count >= 100)
                {
                    context.Response.StatusCode = 429;
                    await context.Response.WriteAsync("Too many requests. Please try again later.");
                    return;
                }
                await cache.SetAsync(key, count + 1, TimeSpan.FromMinutes(1));
            }
            catch
            {
                // If Redis is unavailable, allow the request through
            }
        }
        await _next(context);
    }
}
