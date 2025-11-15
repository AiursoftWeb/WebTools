using Aiursoft.WebTools.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebTools.OfficialPlugins;

public class SupportForwardHeadersPlugin(bool trustAnyProxy = false) : IWebAppPlugin
{
    public bool ShouldAddThisPlugin()
    {
        return true;
    }

    public Task PreServiceConfigure(WebApplicationBuilder builder)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            Console.WriteLine("SupportForwardHeadersPlugin has been added. The forward headers X-Forwarded-For, X-Forwarded-Proto and X-Forwarded-Host will be supported.");
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;

            // Only loopback proxies are allowed by default.
            // Clear that restriction because forwarders are enabled by explicit
            // configuration.
            if (trustAnyProxy)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(@"This application is configured to trust any reverse proxy.
This is because this application is deployed in Docker. Usually when an app was deployed in docker, we will use a reverse proxy, like Caddy.
However it's hard to setup the internal network between Caddy and this app. So we trust any proxy here.
If this app's endpoint couldn't be accessed without the proxy, then it's still safe to serve this app.");
                Console.ResetColor();
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            }
        });
        return Task.CompletedTask;
    }

    public Task PostServiceConfigure(WebApplicationBuilder builder)
    {
        return Task.CompletedTask;
    }

    public Task AppConfiguration(WebApplication builder)
    {
        builder.UseForwardedHeaders();
        return Task.CompletedTask;
    }
}
