using Aiursoft.WebTools.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Aiursoft.WebTools.OfficialPlugins;

public class HandleRobotsPlugin : IWebAppPlugin
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
        return Task.CompletedTask;
    }

    public Task AppConfiguration(WebApplication builder)
    {
        builder.UseMiddleware<HandleRobotsMiddleware>();
        Console.WriteLine("Handle robots plugin has been added. A robots.txt file will be served at /robots.txt.");
        return Task.CompletedTask;
    }
}

public class HandleRobotsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/robots.txt")
        {
            context.Response.ContentType = "text/plain";
            // Allow everything to be indexed. Allow all search engines to index this site.
            await context.Response.WriteAsync("User-agent: *\nDisallow: ");
        }
        else
        {
            await next(context);
        }
    }
}