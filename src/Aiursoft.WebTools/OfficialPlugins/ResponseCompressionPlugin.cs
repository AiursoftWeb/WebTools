using Aiursoft.WebTools.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;

namespace Aiursoft.WebTools.OfficialPlugins;

public class ResponseCompressionPlugin : IWebAppPlugin
{
    public bool ShouldAddThisPlugin()
    {
        return true;
    }

    public Task PreServiceConfigure(WebApplicationBuilder builder)
    {
        return Task.CompletedTask;
    }

    public Task PostServiceConfigure(WebApplicationBuilder builder)
    {
        var noResponseCompressionServiceAdded = builder.Services.All(s => s.ServiceType != typeof(IResponseCompressionProvider));
        
        // If there is no response compression service added, add one.
        if (noResponseCompressionServiceAdded)
        {
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
            });
            Console.WriteLine("Response compression plugin has been added. Brotli compression provider has been added.");
        }
        
        return Task.CompletedTask;
    }

    public Task AppConfiguration(WebApplication builder)
    {
        builder.UseResponseCompression();
        Console.WriteLine("Response compression middleware has been added.");
        return Task.CompletedTask;
    }
}