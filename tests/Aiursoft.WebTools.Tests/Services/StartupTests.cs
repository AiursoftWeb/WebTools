using System.Net;
using Aiursoft.CSTools.Tools;
using Aiursoft.WebTools.Abstractions.Models;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebTools.Tests.Services;

[TestClass]
public class StartupTests
{
    [TestMethod]
    public async Task TestHelloWorld()
    {
        var port = Network.GetAvailablePort();
        var app = await Extends.AppAsync<TestStartup>([], port);

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

[TestClass]
public class RateLimitTest
{
    [TestMethod]
    public async Task TestRateLimit()
    {
        var port = Network.GetAvailablePort();
        var app = await Extends.AppAsync<TestStartupForRateLimit>([], port);

        await app.StartAsync();
        var client = new HttpClient();
        for (var i = 0; i < 20; i++) // Make 20 requests, all should be OK.
        {
            var response = await client.GetAsync($"http://localhost:{port}/");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
        
        // The 21st request should be blocked.
        var lastResponse = await client.GetAsync($"http://localhost:{port}/");
        Assert.AreEqual(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        
        LimitPerMin.ReleaseAllRecords();
        var responseAfterRelease = await client.GetAsync($"http://localhost:{port}/");
        Assert.AreEqual(HttpStatusCode.OK, responseAfterRelease.StatusCode);
    }
    
    [TestMethod]
    public async Task TestRateLimitOnlyOne()
    {
        var port = Network.GetAvailablePort();
        var app = await Extends.AppAsync<TestStartupForRateLimit>([], port);

        await app.StartAsync();
        var client = new HttpClient();
        var firstResponse = await client.GetAsync($"http://localhost:{port}/only-one");
        Assert.AreEqual(HttpStatusCode.OK, firstResponse.StatusCode);
        
        var secondResponse = await client.GetAsync($"http://localhost:{port}/only-one");
        Assert.AreEqual(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
        
        LimitPerMin.GlobalEnabled = false;
        var thirdResponse = await client.GetAsync($"http://localhost:{port}/only-one");
        Assert.AreEqual(HttpStatusCode.OK, thirdResponse.StatusCode);
    }
}

public class TestStartupForRateLimit : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment,
        IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(TestStartupForRateLimit).Assembly);
    }

    public void Configure(WebApplication app)
    {
        app.MapDefaultControllerRoute();
    }
}

public class TestController : ControllerBase
{
    [HttpGet]
    [LimitPerMin(20)]
    [Route("/")]
    public string Index()
    {
        return "Hello World!";
    }
    
    [HttpGet]
    [LimitPerMin(1)]
    [Route("/only-one")]
    public string IndexOnlyOne()
    {
        return "Hello World!";
    }
}