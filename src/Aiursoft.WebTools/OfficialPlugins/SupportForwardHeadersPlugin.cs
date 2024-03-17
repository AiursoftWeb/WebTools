using Aiursoft.WebTools.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebTools.OfficialPlugins;

public class SupportForwardHeadersPlugin : IWebAppPlugin
{
    public bool ShouldAddThisPlugin()
    {
        return true;
    }

    public Task PreConfigure(WebApplicationBuilder builder)
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
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
        return Task.CompletedTask;
    }

    public Task AppConfiguration(WebApplication builder)
    {
        builder.UseForwardedHeaders();
        return Task.CompletedTask;
    }
}