using System.Net.WebSockets;
using System.Text;
using AiurObserver;

namespace Aiursoft.WebTools.Services;

public class AiurWebSocket : AsyncObservable<string>
{
    private bool _dropped;
    private readonly WebSocket _ws;
    
    public bool Connected => !_dropped && _ws.State == WebSocketState.Open;
    
    public string LastMessage { get; private set; } = string.Empty;
    
    internal AiurWebSocket(WebSocket ws)
    {
        _ws = ws;
    }
    
    public async Task Send(string message, CancellationToken token = default)
    {
        try
        {
            if (_dropped)
            {
                throw new WebSocketException("WebSocket is dropped!");
            }
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
            await _ws.SendAsync(buffer, WebSocketMessageType.Text, true, token);
        }
        catch (WebSocketException)
        {
            _dropped = true;
        }
    }

    public async Task Listen(CancellationToken token = default)
    {
        try
        {
            var buffer = new ArraySegment<byte>(new byte[4 * 1024]);
            while (true)
            {
                var message = await _ws.ReceiveAsync(buffer, token);
                switch (message.MessageType)
                {
                    case WebSocketMessageType.Text:
                    {
                        var messageBytes = buffer.Skip(buffer.Offset).Take(message.Count).ToArray();
                        var messageString = Encoding.UTF8.GetString(messageBytes);
                        Broadcast(messageString);
                        LastMessage = messageString;
                        break;
                    }
                    case WebSocketMessageType.Close:
                        await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Close because of error.", token);
                        _dropped = true;
                        return;
                    case WebSocketMessageType.Binary:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                if (_ws.State == WebSocketState.Open)
                {
                    continue;
                }

                _dropped = true;
                return;
            }
        }
        catch (WebSocketException)
        {
            _dropped = true;
        }
    }

    public Task Close(CancellationToken token = default)
    {
        if (_ws.State == WebSocketState.Open)
        {
            return _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Close because of error.", token);
        }

        _dropped = true;
        
        return Task.CompletedTask;
    }
}