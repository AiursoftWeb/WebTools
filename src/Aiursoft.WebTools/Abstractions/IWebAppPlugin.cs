using Microsoft.AspNetCore.Builder;

namespace Aiursoft.WebTools.Abstractions;

public interface IWebAppPlugin
{
    bool ShouldAddThisPlugin();
    
    Task PreConfigure(WebApplicationBuilder builder);
    
    Task AppConfiguration(WebApplication builder);
}