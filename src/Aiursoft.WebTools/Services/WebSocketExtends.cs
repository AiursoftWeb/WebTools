using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;

namespace Aiursoft.WebTools.Services;

public static class WebSocketExtends
{
    public static async Task<AiurWebSocket> AcceptWebSocketClient(this HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            throw new InvalidOperationException("This request is not a WebSocket request!");
        }
        return new AiurWebSocket(await context.WebSockets.AcceptWebSocketAsync());
    }
    
    public static async Task<AiurWebSocket> ConnectAsWebSocketServer(this string endpoint)
    {
        var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri(endpoint), CancellationToken.None);
        return new AiurWebSocket(client);
    }
}