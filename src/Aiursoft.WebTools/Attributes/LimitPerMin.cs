using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aiursoft.WebTools.Attributes;

public class LimitPerMin(int limit = 30) : ActionFilterAttribute
{
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
        logger.LogInformation($"Got a direct HTTP Request. IP: {tcpIp}");
        return tcpIp;
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
        if (MemoryDictionary.TryGetValue(key, out value) && value.count >= limit)
        {
            logger.LogWarning($"Rate limit exceeded for {key}");
            context.Result = new StatusCodeResult((int)HttpStatusCode.TooManyRequests);
            return;
        }
        
        logger.LogInformation($"Rate limit remaining for {key}: {limit - (MemoryDictionary.TryGetValue(key, out value) ? value.count : 0)}");
        context.HttpContext.Response.Headers.Append("x-rate-limit-limit", "1m");
        context.HttpContext.Response.Headers.Append("x-rate-limit-remaining", (limit - (MemoryDictionary.TryGetValue(key, out value) ? value.count : 0)).ToString());
    }
}