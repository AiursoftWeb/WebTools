using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace Aiursoft.WebTools.Attributes;

/// <summary>
/// An attribute that limits the number of requests per minute. Add this attribute to an ASP.NET Core controller or action method to limit requests per minute.
/// </summary>
/// <param name="limit">The request limit per minute. If set to 1, only the first request is allowed within a minute.</param>
public class LimitPerMin(int limit = 30) : ActionFilterAttribute
{
    private readonly int _actualLimit = limit + 1;
    public static bool GlobalEnabled = true;

    /// <summary>
    /// A private helper class to implement atomic increment in IMemoryCache.
    /// </summary>
    private class AtomicCounter
    {
        // ReSharper disable once RedundantDefaultMemberInitializer
        private int _count = 0;

        /// <summary>
        /// Increments the counter in a thread-safe manner and returns the new value.
        /// </summary>
        public int Increment()
        {
            // Interlocked.Increment guarantees the atomicity of the operation and returns the incremented value.
            return Interlocked.Increment(ref _count);
        }
    }

    private string GetIp(HttpContext context, ILogger<LimitPerMin> logger)
    {
        // Always only trust RemoteIpAddress.
        // ForwardedHeadersMiddleware has already ensured this IP is the client's real IP.
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        logger.LogTrace($"Rate limiting for client IP: {clientIp}");
        return clientIp;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (!GlobalEnabled)
        {
            return;
        }

        // Get IMemoryCache and ILogger from Dependency Injection (DI)
        var logger = context.HttpContext.RequestServices.GetService<ILogger<LimitPerMin>>()!;
        var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();

        if (cache == null)
        {
            throw new InvalidOperationException("IMemoryCache service is not registered. Rate limiting is disabled.");
        }

        var path = context.HttpContext.Request.Path.ToString();
        var ip = GetIp(context.HttpContext, logger);
        var key = ip + path;

        // Use GetOrCreate to atomically get or create the counter
        // GetOrCreate ensures that even in concurrent cases, only one AtomicCounter instance is created for each key.
        var counter = cache.GetOrCreate(key, entry =>
        {
            // Set the absolute expiration time to 1 minute from now.
            // This creates a "fixed window": timing starts from the first request, and the window closes (entry cleared) after 1 minute.
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return new AtomicCounter();
        });

        // Atomically increase the count
        // counter is the same instance obtained from the cache
        var currentCount = counter!.Increment();

        // Check the limit
        if (currentCount >= _actualLimit)
        {
            logger.LogWarning($"Rate limit exceeded for {key} (Count: {currentCount})");
            context.Result = new StatusCodeResult((int)HttpStatusCode.TooManyRequests);
            return;
        }

        // Update logs and response headers
        var remaining = _actualLimit - currentCount;
        if (remaining > 10)
        {
            logger.LogTrace(
                $"Rate limit remaining for {key}: {remaining}");
        }
        else
        {
            logger.LogWarning(
                $"Rate limit remaining for {key}: {remaining}");
        }

        context.HttpContext.Response.Headers.Append("x-rate-limit-limit", "1m");
        context.HttpContext.Response.Headers.Append("x-rate-limit-remaining", remaining.ToString());
    }
}
