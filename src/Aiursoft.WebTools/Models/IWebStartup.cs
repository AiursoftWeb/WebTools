using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebTools.Models;

public interface IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IServiceCollection services);

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env);
}