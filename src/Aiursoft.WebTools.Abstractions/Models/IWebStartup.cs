using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aiursoft.WebTools.Abstractions.Models;

public interface IWebStartup
{
    public void ConfigureServices(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        IServiceCollection services);

    public void ConfigureLogging(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILoggingBuilder logging)
    {
    }

    public void Configure(WebApplication app);
}
