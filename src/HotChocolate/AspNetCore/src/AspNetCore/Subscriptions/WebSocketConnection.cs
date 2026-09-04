using System.Buffers;
using System.Net.WebSockets;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Transport.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static System.Net.WebSockets.WebSocketMessageType;
using static HotChocolate.AspNetCore.Properties.AspNetCoreResources;
using static HotChocolate.Transport.Sockets.SocketDefaults;
using static HotChocolate.Transport.Sockets.WellKnownProtocols;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class WebSocketConnection : ISocketConnection
{
    // Bounds the close handshake on the message-too-big path. The Close frame is
    // sent first, so a well-behaved client still sees 1009 (MessageTooBig), but
    // WebSocket.CloseAsync then drains the peer's remaining frames until its
    // answering Close frame arrives. A peer that keeps streaming and never answers
    // must not keep the connection open, so the drain is cut off after this
    // timeout, which aborts the socket.
    private static readonly TimeSpan s_closeTimeout = TimeSpan.FromSeconds(5);

    private readonly IProtocolHandler[] _protocolHandlers;
    private readonly int _maxAllowedMessageSize;
    private WebSocket? _webSocket;
    private bool _disposed;

    public WebSocketConnection(HttpContext httpContext, int maxAllowedMessageSize)
    {
        HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAllowedMessageSize);

        var executor = (IRequestExecutor)httpContext.Items[WellKnownContextData.RequestExecutor]!;
        _protocolHandlers = executor.Services.GetServices<IProtocolHandler>().ToArray();
        _maxAllowedMessageSize = maxAllowedMessageSize;
    }

    public bool IsClosed => _webSocket.IsClosed();

    public HttpContext HttpContext { get; }

    public IServiceProvider RequestServices => HttpContext.RequestServices;

    public CancellationToken ApplicationStopping
        => RequestServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

    public CancellationToken RequestAborted => HttpContext.RequestAborted;

    public IDictionary<string, object?> ContextData { get; } = new Dictionary<string, object?>();

    public async Task<IProtocolHandler?> TryAcceptConnection()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WebSocketConnection));
        }

        var webSocketManager = HttpContext.WebSockets;

        if (webSocketManager.WebSocketRequestedProtocols.Count > 0)
        {
            foreach (var protocolHandler in _protocolHandlers)
            {
                if (webSocketManager.WebSocketRequestedProtocols.Contains(protocolHandler.Name))
                {
                    _webSocket = await webSocketManager.AcceptWebSocketAsync(protocolHandler.Name);
                    return protocolHandler;
                }
            }
        }

        using var socket = await webSocketManager.AcceptWebSocketAsync();
        await socket.CloseOutputAsync(
            WebSocketCloseStatus.ProtocolError,
            $"Expected the {GraphQL_Transport_WS} or {GraphQL_WS} protocol.",
            CancellationToken.None);
        _webSocket = null;
        return null;
    }

    public ValueTask SendAsync(
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken = default)
    {
        var webSocket = _webSocket;

        if (_disposed || webSocket.IsClosed())
        {
            return default;
        }

        return webSocket.SendAsync(message, Text, true, cancellationToken);
    }

    public async Task<bool> ReadMessageAsync(
        IBufferWriter<byte> writer,
        CancellationToken cancellationToken = default)
    {
        var webSocket = _webSocket;

        if (_disposed || webSocket.IsClosed())
        {
            return false;
        }

        try
        {
            long size = 0;
            ValueWebSocketReceiveResult socketResult;

            do
            {
                if (webSocket.IsClosed())
                {
                    break;
                }

                var memory = writer.GetMemory(BufferSize);
                socketResult = await webSocket.ReceiveAsync(memory, cancellationToken);
                writer.Advance(socketResult.Count);
                size += socketResult.Count;

                // The message size must be bounded here, while the frames are still
                // being received, because everything read is buffered into the pipe
                // until the end of the message. Pipe backpressure only engages on
                // flush, and the receiver only flushes after the end of the message,
                // so a single oversized message would otherwise be buffered in full.
                if (size > _maxAllowedMessageSize)
                {
                    await CloseMessageTooBigAsync(cancellationToken);
                    return false;
                }
            } while (!socketResult.EndOfMessage);

            return size > 0;
        }
        catch
        {
            // swallow exception, there's nothing we can reasonably do.
            return false;
        }
    }

    public async ValueTask CloseAsync(
       string message,
       ConnectionCloseReason reason,
       CancellationToken cancellationToken = default)
    {
        var webSocket = _webSocket;

        if (_disposed || webSocket.IsClosed())
        {
            return;
        }

        try
        {
            await webSocket.CloseAsync(
                MapCloseStatus(reason),
                message,
                cancellationToken);
        }
        catch
        {
            // we do not throw here ...
        }
        finally
        {
            Dispose();
        }
    }

    public async ValueTask CloseAsync(
        string message,
        int reason,
        CancellationToken cancellationToken = default)
    {
        var webSocket = _webSocket;

        if (_disposed || webSocket.IsClosed())
        {
            return;
        }

        try
        {
            await webSocket.CloseAsync(
                (WebSocketCloseStatus)reason,
                message,
                cancellationToken);
        }
        catch
        {
            // we do not throw here ...
        }
        finally
        {
            Dispose();
        }
    }

    private async Task CloseMessageTooBigAsync(CancellationToken cancellationToken)
    {
        using var closeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        closeCts.CancelAfter(s_closeTimeout);

        await CloseAsync(
            WebSocketConnection_MessageTooBig,
            ConnectionCloseReason.MessageTooBig,
            closeCts.Token);
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
            _ => WebSocketCloseStatus.Empty,
        };

    public void Dispose()
    {
        if (!_disposed)
        {
            _webSocket?.Dispose();
            _webSocket = null;
            _disposed = true;
        }
    }
}
