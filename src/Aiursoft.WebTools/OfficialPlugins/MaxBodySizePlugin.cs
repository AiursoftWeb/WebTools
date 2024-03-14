using Aiursoft.WebTools.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Aiursoft.WebTools.OfficialPlugins;

public class MaxBodySizePlugin : IWebAppPlugin
{
    public bool ShouldAddThisPlugin()
    {
        return true;
    }
    
    public Task PreConfigure(WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = null;
        });
        return Task.CompletedTask;
    }

    public Task AppConfiguration(WebApplication builder)
    {
        return Task.CompletedTask;
    }
}