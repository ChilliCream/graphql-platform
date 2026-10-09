using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using HotChocolate.AspNetCore.Subscriptions.Protocols;
using HotChocolate.Features;
using HotChocolate.Transport.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static HotChocolate.AspNetCore.Properties.AspNetCorePipelineResources;
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

    public WebSocketConnection(
        HttpContext httpContext,
        IRequestExecutor executor,
        int maxAllowedMessageSize)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAllowedMessageSize);

        HttpContext = httpContext;
        _protocolHandlers = executor.Schema.Services.GetServices<IProtocolHandler>().ToArray();
        _maxAllowedMessageSize = maxAllowedMessageSize;
    }

    public bool IsClosed => _webSocket.IsClosed();

    // True while the server still owes a Close frame to the peer. Unlike IsClosed,
    // this stays true when the peer has half-closed (CloseReceived) so the close
    // path can send the answering Close frame required by RFC 6455 5.5.1.
    public bool RequiresClose => !_disposed && RequiresCloseFrame(_webSocket);

    public HttpContext HttpContext { get; }

    public IServiceProvider RequestServices => HttpContext.RequestServices;

    public CancellationToken ApplicationStopping
        => RequestServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

    public CancellationToken RequestAborted => HttpContext.RequestAborted;

    public bool IsConnected { get; internal set; }

    public bool ConnectionInitReceived { get; internal set; }

    public IFeatureCollection Features { get; } = new FeatureCollection();

    public async Task<IProtocolHandler?> TryAcceptConnection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var webSocketManager = HttpContext.WebSockets;

        // The client lists its sub-protocols in preference order (RFC 6455 4.1), so the
        // first requested protocol that has a registered handler is accepted.
        foreach (var requestedProtocol in webSocketManager.WebSocketRequestedProtocols)
        {
            if (TryGetProtocolHandler(requestedProtocol, out var protocolHandler))
            {
                _webSocket = await webSocketManager.AcceptWebSocketAsync(protocolHandler.Name);
                return protocolHandler;
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

        return webSocket.SendAsync(message, WebSocketMessageType.Text, true, cancellationToken);
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

        if (_disposed || !RequiresCloseFrame(webSocket))
        {
            return;
        }

        try
        {
            await SendCloseFrameAsync(
                webSocket,
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

        if (_disposed || !RequiresCloseFrame(webSocket))
        {
            return;
        }

        try
        {
            await SendCloseFrameAsync(
                webSocket,
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

    // A Close frame still needs to be sent while the socket is Open, or while the
    // peer has half-closed (CloseReceived) and the server has not answered yet.
    // RFC 6455 5.5.1 requires the server to answer a received Close frame with a
    // Close frame of its own; otherwise the peer's clean close degrades to an
    // abnormal closure (1006). The shared WebSocketExtensions.IsClosed treats
    // CloseReceived as closed for the read and keep-alive paths, so the close
    // path tracks this condition separately here.
    private static bool RequiresCloseFrame([NotNullWhen(true)] WebSocket? webSocket)
        => webSocket?.State is WebSocketState.Open or WebSocketState.CloseReceived;

    private static Task SendCloseFrameAsync(
        WebSocket webSocket,
        WebSocketCloseStatus closeStatus,
        string message,
        CancellationToken cancellationToken)
    {
        var reason = TruncateCloseReason(message);

        // When the peer already sent its Close frame (CloseReceived), only the
        // answering Close frame is sent. CloseAsync would additionally wait for a
        // peer Close that will never arrive, so CloseOutputAsync is used instead.
        return webSocket.State is WebSocketState.CloseReceived
            ? webSocket.CloseOutputAsync(closeStatus, reason, cancellationToken)
            : webSocket.CloseAsync(closeStatus, reason, cancellationToken);
    }

    // A Close frame reason is limited to 123 UTF-8 bytes (RFC 6455 5.5). The
    // WebSocket close APIs throw when the status description exceeds this, which
    // would swallow the close and leak the connection. A developer-supplied reason
    // (for example a rejection message) can be arbitrarily long, so it is truncated
    // on a rune boundary here to stay valid UTF-8 and within the limit.
    private const int MaxCloseReasonBytes = 123;

    private static string TruncateCloseReason(string message)
    {
        if (string.IsNullOrEmpty(message)
            || Encoding.UTF8.GetByteCount(message) <= MaxCloseReasonBytes)
        {
            return message;
        }

        var byteCount = 0;
        var length = 0;

        foreach (var rune in message.EnumerateRunes())
        {
            if (byteCount + rune.Utf8SequenceLength > MaxCloseReasonBytes)
            {
                break;
            }

            byteCount += rune.Utf8SequenceLength;
            length += rune.Utf16SequenceLength;
        }

        return message[..length];
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

    private bool TryGetProtocolHandler(
        string protocolName,
        [NotNullWhen(true)] out IProtocolHandler? protocolHandler)
    {
        foreach (var handler in _protocolHandlers)
        {
            if (string.Equals(handler.Name, protocolName, StringComparison.Ordinal))
            {
                protocolHandler = handler;
                return true;
            }
        }

        protocolHandler = null;

        return false;
    }

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
