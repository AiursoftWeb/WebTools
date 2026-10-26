using System.Net;
using Aiursoft.CSTools.Tools;
using Aiursoft.WebTools.Abstractions;
using Aiursoft.WebTools.Abstractions.Models;
using Aiursoft.WebTools.Attributes;
using Aiursoft.WebTools.OfficialPlugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.WebTools.Tests.Services;

[TestClass]
public class MacroRateLimitTests
{
    private static async Task<WebApplication> StartAsync(bool? enabled = true, int permits = 20,
        int perIpConcurrency = 2, int concurrency = 4, int windowSeconds = 60, bool defaultPlugins = false)
    {
        var args = new List<string>
        {
            // Keep the explicit loopback trust policy even when a runner enables automatic cloud forwarding.
            "--ForwardedHeaders_Enabled", "false",
            "--MacroRateLimit:PermitLimit", permits.ToString(),
            "--MacroRateLimit:WindowSeconds", windowSeconds.ToString(),
            "--MacroRateLimit:PerIpConcurrencyLimit", perIpConcurrency.ToString(),
            "--MacroRateLimit:ConcurrencyLimit", concurrency.ToString()
        };
        if (enabled.HasValue)
        {
            args.AddRange(["--MacroRateLimit:Enabled", enabled.Value.ToString()]);
        }
        var app = await Extends.AppAsync<MacroRateLimitStartup>(args.ToArray(), Network.GetAvailablePort(),
            plugins: defaultPlugins ? null : new List<IWebAppPlugin>
            {
                new AlwaysAddMemoryCachePlugin(),
                new SupportForwardHeadersPlugin()
            });
        await app.StartAsync();
        return app;
    }

    private static HttpClient Client(WebApplication app) => new() { BaseAddress = new Uri(app.Urls.Single().Replace("0.0.0.0", "localhost")) };

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string path, string ip = "192.0.2.1")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Forwarded-For", ip);
        return await client.SendAsync(request);
    }

    [TestMethod]
    public async Task DisabledByDefaultAndExplicitlyDisabled()
    {
        foreach (var enabled in new bool?[] { null, false })
        {
            await using var app = await StartAsync(enabled, permits: 1);
            using var client = Client(app);
            for (var i = 0; i < 5; i++)
            {
                using var response = await GetAsync(client, $"/macro/{i}");
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }
    }

    [TestMethod]
    public async Task ExplicitMiddlewareWorksWithDefaultPluginsAndKeepsHealthAvailable()
    {
        await using var app = await StartAsync(permits: 1, defaultPlugins: true);
        using var client = Client(app);
        using var first = await GetAsync(client, "/macro/a");
        using var rejected = await GetAsync(client, "/macro/b");
        using var robots = await GetAsync(client, "/robots.txt");
        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, robots.StatusCode);
        if (EntryExtends.IsInDocker())
        {
            using var health = await GetAsync(client, "/health");
            Assert.AreEqual(HttpStatusCode.OK, health.StatusCode);
        }
    }

    [TestMethod]
    public async Task CrossUrlBudgetRejectsBeforeControllerDependenciesAndSeparatesClients()
    {
        await using var app = await StartAsync(permits: 2);
        using var client = Client(app);
        using var first = await GetAsync(client, "/macro/article-a");
        using var second = await GetAsync(client, "/macro/tags-b");
        using var rejected = await GetAsync(client, "/macro/article-c");
        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, second.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.IsTrue(rejected.Headers.RetryAfter!.Delta > TimeSpan.Zero);
        Assert.IsTrue(rejected.Headers.CacheControl!.NoStore);
        Assert.AreEqual("Too many requests. Please try again later.", await rejected.Content.ReadAsStringAsync());
        Assert.AreEqual(2, app.Services.GetRequiredService<MacroRequestState>().Constructed);
        using var otherClient = await GetAsync(client, "/macro/article-c", "192.0.2.2");
        Assert.AreEqual(HttpStatusCode.OK, otherClient.StatusCode);
    }

    [TestMethod]
    public async Task MappedIpv4SharesBudgetAndUntrustedForwardedHeadersCannotChangeIt()
    {
        await using var app = await StartAsync(permits: 1);
        using var client = Client(app);
        using var first = await GetAsync(client, "/macro/a");
        using var mapped = await GetAsync(client, "/macro/b", "::ffff:192.0.2.1");
        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, mapped.StatusCode);

        // Only the loopback proxy is trusted: a second forwarded hop cannot supply a new client identity.
        using var chain = await GetAsync(client, "/macro/c", "198.51.100.1, 192.0.2.1");
        Assert.AreEqual(HttpStatusCode.TooManyRequests, chain.StatusCode);
    }

    [TestMethod]
    public async Task PerIpConcurrencyRejectsWithoutBlockingOtherClientsAndReleasesPermits()
    {
        await using var app = await StartAsync(perIpConcurrency: 1);
        using var client = Client(app);
        var state = app.Services.GetRequiredService<MacroRequestState>();
        var held = GetAsync(client, "/macro/hold-a");
        try
        {
            Assert.IsTrue(await state.Arrived.WaitAsync(TimeSpan.FromSeconds(10)));
            using var rejected = await GetAsync(client, "/macro/b");
            using var otherClient = await GetAsync(client, "/macro/c", "192.0.2.2");
            Assert.AreEqual(HttpStatusCode.TooManyRequests, rejected.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, otherClient.StatusCode);
            Assert.AreEqual(2, state.Constructed);
        }
        finally
        {
            state.Release.TrySetResult();
            using var response = await held;
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
        using var recovered = await GetAsync(client, "/macro/d");
        Assert.AreEqual(HttpStatusCode.OK, recovered.StatusCode);
    }

    [TestMethod]
    public async Task ApplicationConcurrencyIsSharedAcrossClientIps()
    {
        await using var app = await StartAsync(concurrency: 2);
        using var client = Client(app);
        var state = app.Services.GetRequiredService<MacroRequestState>();
        var first = GetAsync(client, "/macro/hold-a");
        var second = GetAsync(client, "/macro/hold-b", "192.0.2.2");
        try
        {
            Assert.IsTrue(await state.Arrived.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.IsTrue(await state.Arrived.WaitAsync(TimeSpan.FromSeconds(10)));
            using var rejected = await GetAsync(client, "/macro/c", "192.0.2.3");
            Assert.AreEqual(HttpStatusCode.TooManyRequests, rejected.StatusCode);
            Assert.AreEqual(2, state.Constructed);
        }
        finally
        {
            state.Release.TrySetResult();
            using var firstResponse = await first;
            using var secondResponse = await second;
        }
        using var recovered = await GetAsync(client, "/macro/d", "192.0.2.3");
        Assert.AreEqual(HttpStatusCode.OK, recovered.StatusCode);
    }

    [TestMethod]
    public async Task RequestBudgetRecoversAfterWindowExpires()
    {
        await using var app = await StartAsync(permits: 1, windowSeconds: 1);
        using var client = Client(app);
        using var first = await GetAsync(client, "/macro/a");
        using var rejected = await GetAsync(client, "/macro/b");
        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        await Task.Delay(rejected.Headers.RetryAfter!.Delta!.Value + TimeSpan.FromMilliseconds(100));
        using var recovered = await GetAsync(client, "/macro/c");
        Assert.AreEqual(HttpStatusCode.OK, recovered.StatusCode);
    }

    [TestMethod]
    public async Task ExistingPerPathAttributeStillApplies()
    {
        var previousEnabled = LimitPerMin.GlobalEnabled;
        var previousTesting = LimitPerMin.KeepFunctioningInUnitTest;
        try
        {
            LimitPerMin.GlobalEnabled = true;
            LimitPerMin.KeepFunctioningInUnitTest = true;
            await using var app = await StartAsync();
            using var client = Client(app);
            using var first = await GetAsync(client, "/macro-old/a");
            using var rejected = await GetAsync(client, "/macro-old/a");
            using var differentPath = await GetAsync(client, "/macro-old/b");
            Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);
            Assert.AreEqual(HttpStatusCode.TooManyRequests, rejected.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, differentPath.StatusCode);
        }
        finally
        {
            LimitPerMin.GlobalEnabled = previousEnabled;
            LimitPerMin.KeepFunctioningInUnitTest = previousTesting;
        }
    }
}

public class MacroRateLimitStartup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        services.AddMacroRateLimiting(options => configuration.GetSection("MacroRateLimit").Bind(options));
        services.AddSingleton<MacroRequestState>();
        services.AddScoped<MacroControllerDependency>();
        services.AddControllers().AddApplicationPart(typeof(MacroRateLimitStartup).Assembly);
    }

    public void Configure(WebApplication app)
    {
        app.UseStatusCodePagesWithReExecute("/macro/error");
        app.UseMacroRateLimiting();
        app.MapControllers();
    }
}

public class MacroRequestState
{
    public int Constructed;
    public SemaphoreSlim Arrived { get; } = new(0);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public class MacroControllerDependency
{
    public MacroControllerDependency(MacroRequestState state)
    {
        Interlocked.Increment(ref state.Constructed);
    }
}

public class MacroRateLimitController(MacroControllerDependency dependency, MacroRequestState state) : ControllerBase
{
    [HttpGet("/macro/{id}")]
    public async Task<string> Get(string id)
    {
        _ = dependency;
        if (id.StartsWith("hold-", StringComparison.Ordinal))
        {
            state.Arrived.Release();
            await state.Release.Task.WaitAsync(HttpContext.RequestAborted);
        }
        return "OK";
    }

    [HttpGet("/macro-old/{id}")]
    [LimitPerMin(1)]
    public string OldLimit(string id) => id;
}
