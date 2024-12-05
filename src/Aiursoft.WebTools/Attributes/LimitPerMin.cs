using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aiursoft.WebTools.Attributes;

/// <summary>
/// An attribute that limits the requests per minute. Add this attribute to an ASP.NET Core controller or action method to limit the requests per minute.
/// </summary>
/// <param name="limit">The limit of requests per minute. If this is set to 1, then only the first request in a minute will be allowed.</param>
public class LimitPerMin(int limit = 30) : ActionFilterAttribute
{
    private readonly int _actualLimit = limit + 1;
    public static bool GlobalEnabled = true;
    private static readonly ConcurrentDictionary<string, (int count, DateTime timestamp)> MemoryDictionary = new();

    private string GetIp(HttpContext context, ILogger<LimitPerMin> logger)
    {
        var tcpIp = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var forward = context.Request.Headers["X-Forwarded-For"];
        if (forward.Count > 0 && !string.IsNullOrWhiteSpace(forward[0]))
        {
            logger.LogInformation($"Got an HTTP Forwarded Request. IP: {forward[0]}, TCP IP: {tcpIp}");
            return forward[0]!;
        }
        logger.LogTrace($"Got a direct HTTP Request. IP: {tcpIp}");
        return tcpIp;
    }
    
    public static void ReleaseAllRecords()
    {
        MemoryDictionary.Clear();
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (!GlobalEnabled)
        {
            return;
        }
        var path = context.HttpContext.Request.Path.ToString();
        var logger = context.HttpContext.RequestServices.GetService<ILogger<LimitPerMin>>()!;
        var ip = GetIp(context.HttpContext, logger);
        var key = ip + path;

        // Clean up old entries
        foreach (var entry in MemoryDictionary.Where(x => DateTime.UtcNow - x.Value.timestamp > TimeSpan.FromMinutes(1)).ToList())
        {
            MemoryDictionary.TryRemove(entry.Key, out _);
        }

        if (MemoryDictionary.TryGetValue(key, out var value))
        {
            MemoryDictionary[key] = (++value.count, DateTime.UtcNow);
        }
        else
        {
            MemoryDictionary.TryAdd(key, (1, DateTime.UtcNow));
        }

        // Check limit
        if (MemoryDictionary.TryGetValue(key, out value) && value.count >= _actualLimit)
        {
            logger.LogWarning($"Rate limit exceeded for {key}");
            context.Result = new StatusCodeResult((int)HttpStatusCode.TooManyRequests);
            return;
        }

        // When the request is not very near to the limit, we log it as trace. Otherwise, we log it as warning.
        var remaining = _actualLimit - (MemoryDictionary.TryGetValue(key, out value) ? value.count : 0);
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
        context.HttpContext.Response.Headers.Append("x-rate-limit-remaining", (_actualLimit - (MemoryDictionary.TryGetValue(key, out value) ? value.count : 0)).ToString());
    }
}