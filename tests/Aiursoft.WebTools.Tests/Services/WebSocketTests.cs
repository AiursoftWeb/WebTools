using Aiursoft.AiurObserver;
using Aiursoft.CSTools.Tools;
using Aiursoft.WebTools.Models;
using Aiursoft.WebTools.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.WebTools.Tests.Services;

[TestClass]
public class WebSocketTests
{
    [TestMethod]
    public async Task TestWebSocket()
    {
        var port = Network.GetAvailablePort();
        var app = Extends.App<TestStartup>(Array.Empty<string>(), port, builder =>
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                // Remove the upload limit from Kestrel. If needed, an upload limit can
                // be enforced by a reverse proxy server, like IIS.
                options.Limits.MaxRequestBodySize = null;
            });
        });
        
        await app.StartAsync();

        var client = await $"ws://localhost:{port}/".ConnectAsWebSocketServer();
        
        var count = new MessageCounter<string>();
        client.Subscribe(count);

        var stage = new MessageStageLast<string>();
        client.Subscribe(stage);

        await Task.Factory.StartNew(() => client.Listen());
        
        await client.Send("ping");
        await client.Send("aaaaa");
        await client.Send("bbbbb");
        await client.Send("ccccc");
        await client.Send("ping");

        await Task.Delay(300);
        Assert.AreEqual(2, count.Count);
        Assert.AreEqual("pong", stage.Stage);

        await client.Close();
    }
}

internal class TestStartup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
    }

    public void Configure(WebApplication app)
    {
        app.UseWebSockets();
        app.Use(async (HttpContext context, RequestDelegate _) =>
        {
            ISubscription? subscription = null;
            try
            {
                var client = await context.AcceptWebSocketClient();
                subscription = client
                    .Filter(t => t == "ping")
                    .Map(_ => "pong")
                    .Subscribe(client);

                await client.Listen(context.RequestAborted);
            }
            finally
            {
                subscription?.Unsubscribe();
            }
        });
    }
}