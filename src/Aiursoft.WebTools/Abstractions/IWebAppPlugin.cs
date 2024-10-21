using Microsoft.AspNetCore.Builder;

namespace Aiursoft.WebTools.Abstractions;

public interface IWebAppPlugin
{
    bool ShouldAddThisPlugin();
    
    Task PreServiceConfigure(WebApplicationBuilder builder);
    Task PostServiceConfigure(WebApplicationBuilder builder);
    
    Task AppConfiguration(WebApplication builder);
}