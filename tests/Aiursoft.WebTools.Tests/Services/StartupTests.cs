using System.Net;
using Aiursoft.CSTools.Tools;
using Aiursoft.WebTools.Abstractions.Models;
using Aiursoft.WebTools.Attributes;
using Aiursoft.WebTools.OfficialPlugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly:DoNotParallelize]

namespace Aiursoft.WebTools.Tests.Services;

[TestClass]
public class StartupTests
{
    private static readonly Lazy<IServiceProvider> _servicesLazy = new(() =>
        new ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider());

    private static IHttpClientFactory ClientFactory => _servicesLazy.Value.GetRequiredService<IHttpClientFactory>();

    [TestMethod]
    public async Task TestHelloWorld()
    {
        var port = Network.GetAvailablePort();
        var app = await Extends.AppAsync<TestStartup>([], port);

        await app.StartAsync();
        var client = ClientFactory.CreateClient();
        var response = await client.GetAsync($"http://localhost:{port}/");
        Assert.AreEqual("Hello World!", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task CroatianIsAvailableOnlyInOptedInApplications()
    {
        var enabledPort = Network.GetAvailablePort();
        await using var enabled = await Extends.AppAsync<TestStartup>(["--Localization:EnableCroatian=true"], enabledPort);
        await enabled.StartAsync();
        var defaultPort = Network.GetAvailablePort();
        await using var defaultApp = await Extends.AppAsync<TestStartup>([], defaultPort);
        await defaultApp.StartAsync();

        var enabledCultures = enabled.Services.GetRequiredKeyedService<IReadOnlyDictionary<string, string>>(typeof(LocalizationPlugin));
        var defaultCultures = defaultApp.Services.GetRequiredKeyedService<IReadOnlyDictionary<string, string>>(typeof(LocalizationPlugin));
        Assert.AreEqual(defaultCultures.Count + 1, enabledCultures.Count);
        Assert.AreEqual("Hrvatski", enabledCultures["hr-HR"]);
        Assert.IsFalse(defaultCultures.ContainsKey("hr-HR"));

        using var client = ClientFactory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("hr");
        Assert.AreEqual("hr-HR", await client.GetStringAsync($"http://localhost:{enabledPort}/culture"));
        Assert.AreEqual("en-US", await client.GetStringAsync($"http://localhost:{defaultPort}/culture"));
    }

    [TestMethod]
    public async Task TestMaxBodySizePluginSetsFormOptionsLimit()
    {
        var port = Network.GetAvailablePort();
        var app = await Extends.AppAsync<TestStartup>([], port);

        var formOptions = app.Services.GetRequiredService<IOptions<FormOptions>>().Value;
        Assert.AreEqual(2L * 1024 * 1024 * 1024, formOptions.MultipartBodyLengthLimit);

        await app.StopAsync();
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
        app.MapGet("/culture", (HttpContext context) =>
            context.Features.Get<IRequestCultureFeature>()!.RequestCulture.UICulture.Name);

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
    [TestInitialize]
    public void TestInitialize()
    {
        LimitPerMin.KeepFunctioningInUnitTest = true;
    }

    private static readonly Lazy<IServiceProvider> _servicesLazy = new(() =>
        new ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider());

    private static IHttpClientFactory ClientFactory => _servicesLazy.Value.GetRequiredService<IHttpClientFactory>();

    [TestMethod]
    public async Task TestRateLimit()
    {
        var port = Network.GetAvailablePort();
        var app = await Extends.AppAsync<TestStartupForRateLimit>([], port);

        await app.StartAsync();
        var client = ClientFactory.CreateClient();
        for (var i = 0; i < 20; i++) // Make 20 requests, all should be OK.
        {
            var response = await client.GetAsync($"http://localhost:{port}/");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        // The 21st request should be blocked.
        var lastResponse = await client.GetAsync($"http://localhost:{port}/");
        Assert.AreEqual(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);

        var cache = app.Services.GetRequiredService<IMemoryCache>() as MemoryCache;
        cache!.Clear();
        var responseAfterRelease = await client.GetAsync($"http://localhost:{port}/");
        Assert.AreEqual(HttpStatusCode.OK, responseAfterRelease.StatusCode);
    }

    [TestMethod]
    public async Task TestRateLimitOnlyOne()
    {
        var port = Network.GetAvailablePort();
        var app = await Extends.AppAsync<TestStartupForRateLimit>([], port);

        await app.StartAsync();
        var client = ClientFactory.CreateClient();
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
        services.AddMemoryCache();
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
