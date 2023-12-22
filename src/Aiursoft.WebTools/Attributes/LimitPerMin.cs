using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace Aiursoft.WebTools.Attributes;

public class LimitPerMin : ActionFilterAttribute
{
    private static readonly ConcurrentDictionary<string, (int count, DateTime timestamp)> MemoryDictionary = new();
    private readonly int _limit;

    public LimitPerMin(int limit = 30)
    {
        _limit = limit;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var path = context.HttpContext.Request.Path.ToString();
        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
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
        if (MemoryDictionary.TryGetValue(key, out value) && value.count >= _limit)
        {
            context.Result = new StatusCodeResult((int)HttpStatusCode.TooManyRequests);
            return;
        }

        context.HttpContext.Response.Headers.Add("x-rate-limit-limit", "1m");
        context.HttpContext.Response.Headers.Add("x-rate-limit-remaining", (_limit - (MemoryDictionary.TryGetValue(key, out value) ? value.count : 0)).ToString());
    }
}