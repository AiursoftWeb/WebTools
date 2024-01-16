using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebTools.Abstractions.Models;

public interface IWebStartup
{
    public void ConfigureServices(
        IConfiguration configuration, 
        IWebHostEnvironment environment, 
        IServiceCollection services);

    public void Configure(WebApplication app);
}