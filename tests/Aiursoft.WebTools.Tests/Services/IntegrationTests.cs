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
        var cacheControl = response.Headers.CacheControl?.ToString();
        Assert.IsNotNull(cacheControl);
        CollectionAssert.Contains(cacheControl.Split(',').Select(x => x.Trim()).ToList(), "no-cache");
        
        Assert.IsTrue(response.Content.Headers.Contains("Expires"));
        Assert.AreEqual("-1", response.Content.Headers.GetValues("Expires").First());
    }

    [TestMethod]
    public async Task TestEnforceWebSocket()
    {
        // Test with HTTP request (should fail)
        var response = await _client.GetAsync($"http://localhost:{_port}/websocket-only");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

public class TestStartupForIntegration : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(TestStartupForIntegration).Assembly);
    }

    public void Configure(WebApplication app)
    {
        app.MapControllers();
    }
}

public class IntegrationTestController : ControllerBase
{
    [HttpGet]
    [Route("large-text")]
    public async Task LargeText()
    {
        var text = new string('a', 10000); // 10KB
        Response.ContentType = "text/plain";
        await Response.WriteAsync(text);
    }

    [HttpGet]
    [Route("no-cache")]
    [AiurNoCache]
    public async Task NoCache()
    {
        await Response.WriteAsync("No Cache");
    }

    [HttpGet]
    [Route("websocket-only")]
    [EnforceWebSocket]
    public async Task WebSocketOnly()
    {
        await Response.WriteAsync("WebSocket");
    }

    [HttpGet]
    [Route("info")]
    public async Task Info()
    {
        var isMobile = Request.IsMobileBrowser();
        var isWeChat = Request.IsWeChat();
        var isIos = Request.IsIos();
        var isAndroid = Request.IsAndroid();
        await Response.WriteAsync($"Mobile: {isMobile}, WeChat: {isWeChat}, iPhone: {isIos}, Android: {isAndroid}");
    }

    [HttpGet]
    [Route("culture")]
    public async Task Culture()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture.Name;
        await Response.WriteAsync(culture);
    }

    [HttpGet]
    [Route("ip")]
    public async Task Ip()
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await Response.WriteAsync(ip ?? "unknown");
    }

    [HttpPost]
    [Route("upload")]
    public async Task Upload()
    {
        // Read the whole body
        await Request.Body.CopyToAsync(Stream.Null, CancellationToken.None);
        await Response.WriteAsync("OK");
    }
}