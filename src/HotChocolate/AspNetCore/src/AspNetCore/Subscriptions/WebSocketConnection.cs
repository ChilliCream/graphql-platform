using System.Buffers;
using System.Net.WebSockets;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using static System.Net.WebSockets.WebSocketMessageType;
using static HotChocolate.AspNetCore.Subscriptions.ProtocolNames;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class WebSocketConnection : ISocketConnection
{
    private readonly IProtocolHandler[] _protocolHandlers;
    private const int _maxMessageSize = 512;
    private WebSocket? _webSocket;
    private bool _disposed;

    private WebSocketConnection(HttpContext httpContext)
    {
        HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        _protocolHandlers = httpContext.RequestServices.GetServices<IProtocolHandler>().ToArray();
        Subscriptions = new SubscriptionManager();
    }

    public bool IsClosed => _webSocket is null || _webSocket.CloseStatus.HasValue;

    public HttpContext HttpContext { get; }

    public ISubscriptionManager Subscriptions { get; }

    public IServiceProvider RequestServices => HttpContext.RequestServices;

    public CancellationToken RequestAborted => HttpContext.RequestAborted;

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();

    public async Task<IProtocolHandler?> TryAcceptConnection()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WebSocketConnection));
        }

        WebSocketManager webSocketManager = HttpContext.WebSockets;

        if (webSocketManager.WebSocketRequestedProtocols.Count > 0)
        {
            foreach (IProtocolHandler protocolHandler in _protocolHandlers)
            {
                if (webSocketManager.WebSocketRequestedProtocols.Contains(protocolHandler.Name))
                {
                    _webSocket = await webSocketManager.AcceptWebSocketAsync(protocolHandler.Name);
                    return protocolHandler;
                }
            }
        }

        using WebSocket socket = await webSocketManager.AcceptWebSocketAsync();
        await socket.CloseOutputAsync(
            WebSocketCloseStatus.ProtocolError,
            $"Expected the {GraphQL_Transport_WS} or {GraphQL_WS} protocol.",
            CancellationToken.None);
        _webSocket = null;
        return null;
    }

    public Task SendAsync(ArraySegment<byte> message, CancellationToken cancellationToken)
    {
        WebSocket? webSocket = _webSocket;

        if (_disposed || webSocket is not { State: WebSocketState.Open })
        {
            return Task.CompletedTask;
        }

        return webSocket.SendAsync(message, Text, true, cancellationToken);
    }

    public async Task ReceiveAsync(
        IBufferWriter<byte> writer,
        CancellationToken cancellationToken)
    {
        WebSocket? webSocket = _webSocket;

        if (_disposed || webSocket is not { State: WebSocketState.Open })
        {
            return;
        }

        try
        {
            ValueWebSocketReceiveResult socketResult;
            do
            {
                if (webSocket.State is not WebSocketState.Open)
                {
                    break;
                }

                Memory<byte> memory = writer.GetMemory(_maxMessageSize);
                socketResult = await webSocket.ReceiveAsync(memory, cancellationToken);

                if (socketResult.Count == 0)
                {
                    memory = writer.GetMemory(1);
                    memory.Span[0] = SubscriptionSession.Delimiter;
                    writer.Advance(1);
                    break;
                }

                writer.Advance(socketResult.Count);
            } while (!socketResult.EndOfMessage);
        }
        catch
        {
            // swallow exception, there's nothing we can reasonably do
        }
    }

    public async Task CloseAsync(
       string message,
       ConnectionCloseReason reason,
       CancellationToken cancellationToken)
    {
        try
        {
            WebSocket? webSocket = _webSocket;

            if (_disposed || IsClosed || webSocket is null || webSocket.State != WebSocketState.Open)
            {
                return;
            }

            await webSocket.CloseOutputAsync(
                MapCloseStatus(reason),
                message,
                cancellationToken);

            Dispose();
        }
        catch
        {
            // we do not throw here ...
        }
    }

    private static WebSocketCloseStatus MapCloseStatus(ConnectionCloseReason closeReason)
        => closeReason switch
        {
            ConnectionCloseReason.EndpointUnavailable => WebSocketCloseStatus.EndpointUnavailable,
            ConnectionCloseReason.InternalServerError => WebSocketCloseStatus.InternalServerError,
            ConnectionCloseReason.InvalidMessageType => WebSocketCloseStatus.InvalidMessageType,
            ConnectionCloseReason.InvalidPayloadData => WebSocketCloseStatus.InvalidPayloadData,
            ConnectionCloseReason.MandatoryExtension => WebSocketCloseStatus.MandatoryExtension,
            ConnectionCloseReason.MessageTooBig => WebSocketCloseStatus.MessageTooBig,
            ConnectionCloseReason.NormalClosure => WebSocketCloseStatus.NormalClosure,
            ConnectionCloseReason.PolicyViolation => WebSocketCloseStatus.PolicyViolation,
            ConnectionCloseReason.ProtocolError => WebSocketCloseStatus.ProtocolError,
            _ => WebSocketCloseStatus.Empty
        };

    public void Dispose()
    {
        if (!_disposed)
        {
            Subscriptions.Dispose();
            _webSocket?.Dispose();
            _webSocket = null;
            _disposed = true;
        }
    }

    public static WebSocketConnection New(HttpContext httpContext) => new(httpContext);
}
