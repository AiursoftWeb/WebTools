using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace Aiursoft.WebTools.Attributes;

public class LimitPerMin : ActionFilterAttribute
{
    private static readonly ConcurrentDictionary<string, int> MemoryDictionary = new();
    private static DateTime _lastClearTime = DateTime.UtcNow;

    private readonly int _limit;

    public LimitPerMin(int limit = 30)
    {
        _limit = limit;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        if (DateTime.UtcNow - _lastClearTime > TimeSpan.FromMinutes(1))
        {
            MemoryDictionary.Clear();
            _lastClearTime = DateTime.UtcNow;
        }
        var path = context.HttpContext.Request.Path.ToString();
        var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var key = ip + path;

        if (MemoryDictionary.TryGetValue(key, out var value))
        {
            MemoryDictionary[key] = ++value;
        }
        else
        {
            MemoryDictionary.TryAdd(key, 1);
        }

        if (MemoryDictionary[key] >= _limit)
        {
            context.Result = new StatusCodeResult((int)HttpStatusCode.TooManyRequests);
            return;
        }

        context.HttpContext.Response.Headers.Add("x-rate-limit-limit", "1m");
        context.HttpContext.Response.Headers.Add("x-rate-limit-remaining", (_limit - MemoryDictionary[key]).ToString());
    }
}
