using Aiursoft.CSTools.Tools;
using Aiursoft.WebTools.Abstractions.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.WebTools.Tests.Services;

[TestClass]
public class StartupTests
{
    [TestMethod]
    public async Task TestHelloWorld()
    {
        var port = Network.GetAvailablePort();
        var app = await Extends.AppAsync<TestStartup>(Array.Empty<string>(), port);

        await app.StartAsync();
        var client = new HttpClient();
        var response = await client.GetAsync($"http://localhost:{port}/");
        Assert.AreEqual("Hello World!", await response.Content.ReadAsStringAsync());
    }
}

internal class TestStartup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment,
        IServiceCollection services)
    {
    }

    public void Configure(WebApplication app)
    {
        // Hello world data:
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/")
            {
                await context.Response.WriteAsync("Hello World!");
            }
            else
            {
                await next();
            }
        });
    }
}