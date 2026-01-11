using System.Net;
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
public class IntegrationTests
{
    private HttpClient _client = null!;
    private WebApplication _app = null!;
    private int _port;

    [TestInitialize]
    public async Task Initialize()
    {
        _port = Network.GetAvailablePort();
        _app = await Extends.AppAsync<TestStartupForIntegration>([], _port);
        await _app.StartAsync();
        _client = new HttpClient();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        _client.Dispose();
    }

    [TestMethod]
    public async Task TestRobots()
    {
        var response = await _client.GetAsync($"http://localhost:{_port}/robots.txt");
        var content = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(content, "User-agent: *");
    }

    [TestMethod]
    public async Task TestBrowserDetection()
    {
        // Normal Desktop
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
        var response = await _client.GetAsync($"http://localhost:{_port}/info");
        var content = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(content, "Mobile: False");
        StringAssert.Contains(content, "WeChat: False");

        // iPhone
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (iPhone; CPU iPhone OS 10_3_1 like Mac OS X) AppleWebKit/603.1.30 (KHTML, like Gecko) Version/10.0 Mobile/14E304 Safari/602.1");
        response = await _client.GetAsync($"http://localhost:{_port}/info");
        content = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(content, "Mobile: True");
        StringAssert.Contains(content, "WeChat: False");
        StringAssert.Contains(content, "iPhone: True");

        // WeChat
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Linux; Android 6.0.1; SM-G920V Build/MMB29K; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/52.0.2743.98 Mobile Safari/537.36 MicroMessenger/6.5.4.1000 NetType/WIFI Language/zh_CN");
        response = await _client.GetAsync($"http://localhost:{_port}/info");
        content = await response.Content.ReadAsStringAsync();
        StringAssert.Contains(content, "Mobile: True"); // Android + Mobile in UA
        StringAssert.Contains(content, "WeChat: True");
        StringAssert.Contains(content, "Android: True");
    }

    [TestMethod]
    public async Task TestLocalization()
    {
        _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-CN");
        var response = await _client.GetAsync($"http://localhost:{_port}/culture");
        var content = await response.Content.ReadAsStringAsync();
        Assert.AreEqual("zh-CN", content);

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US");
        response = await _client.GetAsync($"http://localhost:{_port}/culture");
        content = await response.Content.ReadAsStringAsync();
        Assert.AreEqual("en-US", content);
    }

    [TestMethod]
    public async Task TestSecurityHeaders()
    {
        var response = await _client.GetAsync($"http://localhost:{_port}/info");
        Assert.IsTrue(response.Headers.Contains("X-Frame-Options"));
        Assert.AreEqual("SAMEORIGIN", response.Headers.GetValues("X-Frame-Options").First());
        Assert.IsTrue(response.Headers.Contains("Content-Security-Policy"));
        Assert.AreEqual("frame-ancestors 'self'", response.Headers.GetValues("Content-Security-Policy").First());
    }

    [TestMethod]
    public async Task TestForwardedHeaders()
    {
        _client.DefaultRequestHeaders.Add("X-Forwarded-For", "8.8.8.8");
        var response = await _client.GetAsync($"http://localhost:{_port}/ip");
        var content = await response.Content.ReadAsStringAsync();
        Assert.AreEqual("8.8.8.8", content);
    }

    [TestMethod]
    public async Task TestResponseCompression()
    {
        _client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("br");
        var response = await _client.GetAsync($"http://localhost:{_port}/large-text");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        CollectionAssert.Contains(response.Content.Headers.ContentEncoding.ToList(), "br");
    }

    [TestMethod]
    public void TestMemoryCache()
    {
        var cache = _app.Services.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        Assert.IsNotNull(cache);
    }

    [TestMethod]
    public async Task TestAiurNoCache()
    {
        var response = await _client.GetAsync($"http://localhost:{_port}/no-cache");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        CollectionAssert.Contains(response.Headers.CacheControl.ToString().Split(','), "no-cache");
        CollectionAssert.Contains(response.Content.Headers.Expires.ToString().Split(','), "-1");
    }

    [TestMethod]
    public async Task TestEnforceWebSocket()
    {
        // Test with HTTP request (should fail)
        var response = await _client.GetAsync($"http://localhost:{_port}/websocket-only");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        // Test with WebSocket request (mocking IsWebSocketRequest is hard in integration test without real WS client, 
        // but we can at least verify the 400 for non-WS)
        // To test success, we would need to actually initiate a WS handshake.
        // For now, ensuring it blocks non-WS is the key logic of the attribute.
    }
}

public class TestStartupForIntegration : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        services.AddControllers();
    }

    public void Configure(WebApplication app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/large-text", async context =>
            {
                var text = new string('a', 10000); // 10KB
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(text);
            });

            endpoints.MapGet("/no-cache", [Aiursoft.WebTools.Attributes.AiurNoCache] async (context) => 
            {
                await context.Response.WriteAsync("No Cache");
            });

            endpoints.MapGet("/websocket-only", [Aiursoft.WebTools.Attributes.EnforceWebSocket] async (context) =>
            {
                await context.Response.WriteAsync("WebSocket");
            });

            endpoints.MapGet("/info", async context =>
            {
                var isMobile = context.Request.IsMobileBrowser();
                var isWeChat = context.Request.IsWeChat();
                var isIos = context.Request.IsIos();
                var isAndroid = context.Request.IsAndroid();
                await context.Response.WriteAsync($"Mobile: {isMobile}, WeChat: {isWeChat}, iPhone: {isIos}, Android: {isAndroid}");
            });

            endpoints.MapGet("/culture", async context =>
            {
                var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
                await context.Response.WriteAsync(culture);
            });

            endpoints.MapGet("/ip", async context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString();
                await context.Response.WriteAsync(ip ?? "unknown");
            });

            endpoints.MapPost("/upload", async context =>
            {
                // Read the whole body
                await context.Request.Body.CopyToAsync(Stream.Null, CancellationToken.None);
                await context.Response.WriteAsync("OK");
            });
        });
    }
}
