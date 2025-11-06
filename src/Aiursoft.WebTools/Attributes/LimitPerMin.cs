using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory; // 1. 引入 IMemoryCache

namespace Aiursoft.WebTools.Attributes;

/// <summary>
/// 一个限制每分钟请求次数的属性。将此属性添加到 ASP.NET Core 控制器或操作方法以限制每分钟的请求。
/// </summary>
/// <param name="limit">每分钟的请求限制。如果设置为 1，则一分钟内只允许第一个请求。</param>
public class LimitPerMin(int limit = 30) : ActionFilterAttribute
{
    private readonly int _actualLimit = limit + 1;
    public static bool GlobalEnabled = true;

    /// <summary>
    /// 一个私有帮助类，用于在 IMemoryCache 中实现原子递增。
    /// </summary>
    private class AtomicCounter
    {
        // ReSharper disable once RedundantDefaultMemberInitializer
        private int _count = 0;

        /// <summary>
        /// 以线程安全的方式递增计数器并返回新值。
        /// </summary>
        public int Increment()
        {
            // Interlocked.Increment 保证操作的原子性，返回递增后的值。
            return Interlocked.Increment(ref _count);
        }
    }

    private string GetIp(HttpContext context, ILogger<LimitPerMin> logger)
    {
        // 永远只信任 RemoteIpAddress。
        // ForwardedHeadersMiddleware 已经确保了这个 IP 是客户端的真实 IP。
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

        // 2. 从依赖注入 (DI) 中获取 IMemoryCache 和 ILogger
        var logger = context.HttpContext.RequestServices.GetService<ILogger<LimitPerMin>>()!;
        var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>();

        if (cache == null)
        {
            throw new InvalidOperationException("IMemoryCache service is not registered. Rate limiting is disabled.");
        }

        var path = context.HttpContext.Request.Path.ToString();
        var ip = GetIp(context.HttpContext, logger);
        var key = ip + path;

        // 3. 使用 GetOrCreate 来原子性地获取或创建计数器
        // GetOrCreate 确保了即使在并发情况下，每个 key 也只会创建一个 AtomicCounter 实例。
        var counter = cache.GetOrCreate(key, entry =>
        {
            // 设置绝对过期时间为1分钟后。
            // 这创建了一个“固定窗口”：从第一次请求开始计时，1分钟后窗口关闭（条目被清除）。
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return new AtomicCounter();
        });

        // 4. 原子性地增加计数
        // counter 是从缓存中获取的同一个实例
        var currentCount = counter!.Increment();

        // 5. 检查限制
        if (currentCount >= _actualLimit)
        {
            logger.LogWarning($"Rate limit exceeded for {key} (Count: {currentCount})");
            context.Result = new StatusCodeResult((int)HttpStatusCode.TooManyRequests);
            return;
        }

        // 6. 更新日志和响应头
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
