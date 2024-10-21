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
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;
            
            // Only loopback proxies are allowed by default.
            // Clear that restriction because forwarders are enabled by explicit 
            // configuration.
            if (trustAnyProxy)
            {
                Console.WriteLine(@"This application is configured to trust any proxy. 
This is because this application is deployed in Docker. Usually when an app was deployed in docker, we will use a reverse proxy, like Caddy.
However it's hard to configure Caddy to attach real IP to the request. So we trust any proxy here.
If this app's endpoint couldn't be accessed without the proxy, then it's still safe to serve this app.");
                options.KnownNetworks.Clear();
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