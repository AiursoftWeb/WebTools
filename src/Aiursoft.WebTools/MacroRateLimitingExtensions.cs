using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebTools;

/// <summary>
/// Optional application-wide protection, independent of the per-action LimitPerMin attribute.
/// Limits are local to each application process. Register after static files and before MVC.
/// </summary>
public static class MacroRateLimitingExtensions
{
    public static IServiceCollection AddMacroRateLimiting(this IServiceCollection services,
        Action<MacroRateLimitOptions>? configure = null)
    {
        var settings = new MacroRateLimitOptions();
        configure?.Invoke(settings);
        services.AddSingleton(settings);
        if (!settings.Enabled)
        {
            return services;
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.PermitLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.WindowSeconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.PerIpConcurrencyLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(settings.ConcurrencyLimit);

        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(GetClientIp(context), _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                        QueueLimit = 0
                    })),
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetConcurrencyLimiter(GetClientIp(context), _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = settings.PerIpConcurrencyLimit,
                        QueueLimit = 0
                    })),
                PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                    RateLimitPartition.GetConcurrencyLimiter("application", _ => new ConcurrencyLimiterOptions
                    {
                        PermitLimit = settings.ConcurrencyLimit,
                        QueueLimit = 0
                    })));
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var delay)
                    ? Math.Max(1, (int)Math.Ceiling(delay.TotalSeconds))
                    : 1;
                context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);
                context.HttpContext.Response.Headers.CacheControl = "no-store";
                context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                // A response body prevents status-code pages from executing a database-backed error controller.
                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
            };
        });
        return services;
    }

    public static IApplicationBuilder UseMacroRateLimiting(this IApplicationBuilder app)
    {
        return app.ApplicationServices.GetRequiredService<MacroRateLimitOptions>().Enabled
            ? app.UseRateLimiter()
            : app;
    }

    private static string GetClientIp(HttpContext context)
    {
        // Use only the address already validated by ForwardedHeadersMiddleware, never raw request headers.
        var address = context.Connection.RemoteIpAddress;
        return address?.IsIPv4MappedToIPv6 == true
            ? address.MapToIPv4().ToString()
            : address?.ToString() ?? "unknown";
    }
}

public class MacroRateLimitOptions
{
    public bool Enabled { get; set; }
    public int PermitLimit { get; set; } = 300;
    public int WindowSeconds { get; set; } = 60;
    public int PerIpConcurrencyLimit { get; set; } = 4;
    public int ConcurrencyLimit { get; set; } = 32;
}
