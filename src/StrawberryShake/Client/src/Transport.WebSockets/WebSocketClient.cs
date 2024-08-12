using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using StrawberryShake.Properties;

namespace StrawberryShake.Transport.WebSockets;

/// <summary>
/// Represents a client for sending and receiving messages responses over a websocket
/// identified by a URI and name.
/// </summary>
public sealed class WebSocketClient : IWebSocketClient
{
    private const int _maxMessageSize = 1024 * 4;
    private readonly IReadOnlyList<ISocketProtocolFactory> _protocolFactories;
    private readonly ClientWebSocket _socket;
    private ISocketProtocol? _activeProtocol;
    private bool _receiveFinishEventTriggered = false;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of <see cref="WebSocketClient"/>
    /// </summary>
    /// <param name="name">The name of the socket</param>
    /// <param name="protocolFactories">The protocol factories this socket supports</param>
    public WebSocketClient(
        string name,
        IReadOnlyList<ISocketProtocolFactory> protocolFactories)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _protocolFactories = protocolFactories ??
            throw new ArgumentNullException(nameof(protocolFactories));
        _socket = new ClientWebSocket();

        for (var i = 0; i < _protocolFactories.Count; i++)
        {
            _socket.Options.AddSubProtocol(_protocolFactories[i].ProtocolName);
        }
    }

    public event EventHandler? OnConnectionClosed;

    /// <inheritdoc />
    public Uri? Uri { get; set; }

    /// <inheritdoc />
    public ISocketConnectionInterceptor ConnectionInterceptor { get; set; } =
        DefaultSocketConnectionInterceptor.Instance;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public bool IsClosed
    {
        get
        {
            var closed = _disposed
                || _socket.CloseStatus.HasValue
                || _socket.State == WebSocketState.Aborted;

            if (closed && !_receiveFinishEventTriggered)
            {
                _receiveFinishEventTriggered = true;
                OnConnectionClosed?.Invoke(this, EventArgs.Empty);
                ConnectionInterceptor.OnConnectionClosed(this);
            }
            return closed;
        }
    }

    /// <inheritdoc />
    public WebSocket Socket => _socket;

    /// <inheritdoc />
    public async Task<ISocketProtocol> OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WebSocketClient));
        }

        if (Uri is null)
        {
            throw ThrowHelper.SocketClient_URIWasNotSpecified(Name);
        }

        await _socket.ConnectAsync(Uri, cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < _protocolFactories.Count; i++)
        {
            if (_protocolFactories[i].ProtocolName == _socket.SubProtocol)
            {
                _activeProtocol = _protocolFactories[i].Create(this);
                break;
            }
        }

        if (_activeProtocol is null)
        {
            await CloseAsync(
                Resources.SocketClient_FailedToInitializeProtocol,
                SocketCloseStatus.ProtocolError,
                cancellationToken)
                .ConfigureAwait(false);

            throw ThrowHelper.SocketClient_ProtocolNotFound(_socket.SubProtocol ?? "null");
        }

        ConnectionInterceptor.OnConnectionOpened(this);

        await _activeProtocol.InitializeAsync(cancellationToken).ConfigureAwait(false);

        return _activeProtocol;
    }

    /// <inheritdoc />
    public async Task CloseAsync(
        string message,
        SocketCloseStatus closeStatus,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsClosed)
            {
                return;
            }

            if (_activeProtocol is not null)
            {
                try
                {
                    await _activeProtocol.TerminateAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch
                {
                    // In case termination of the protocol failed we still want to close the
                    // socket
                }
            }

            await _socket.CloseOutputAsync(
                MapCloseStatus(closeStatus),
                message,
                cancellationToken)
                .ConfigureAwait(false);

            await DisposeAsync();
        }
        catch
        {
            // we do not throw here ...
        }
    }

    /// <inheritdoc />
    public async ValueTask SendAsync(
        ReadOnlyMemory<byte> message,
        CancellationToken cancellationToken = default)
    {
        if (IsClosed)
        {
            return;
        }

        if (MemoryMarshal.TryGetArray(message, out var buffer))
        {
            await _socket.SendAsync(
                buffer,
                WebSocketMessageType.Text,
                true,
                cancellationToken);
        }
    }

    /// <inheritdoc />
    public async ValueTask ReceiveAsync(
        PipeWriter writer,
        CancellationToken cancellationToken = default)
    {
        if (IsClosed)
        {
            return;
        }

        try
        {
            WebSocketReceiveResult? socketResult = null;
            do
            {
                var memory = writer.GetMemory(_maxMessageSize);
                try
                {
                    if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> buffer))
                    {
                        socketResult = await _socket
                            .ReceiveAsync(buffer, cancellationToken)
                            .ConfigureAwait(false);

                        if (socketResult.Count == 0)
                        {
                            break;
                        }

                        writer.Advance(socketResult.Count);
                    }
                }
                catch
                {
                    break;
                }

                var result = await writer
                    .FlushAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (result.IsCompleted)
                {
                    break;
                }
            } while (socketResult is not { EndOfMessage: true, });
        }
        catch (ObjectDisposedException)
        {
            // we will just stop receiving
        }
    }

    private static WebSocketCloseStatus MapCloseStatus(SocketCloseStatus closeStatus)
        => closeStatus switch
        {
            SocketCloseStatus.EndpointUnavailable => WebSocketCloseStatus.EndpointUnavailable,
            SocketCloseStatus.InternalServerError => WebSocketCloseStatus.InternalServerError,
            SocketCloseStatus.InvalidMessageType => WebSocketCloseStatus.InvalidMessageType,
            SocketCloseStatus.InvalidPayloadData => WebSocketCloseStatus.InvalidPayloadData,
            SocketCloseStatus.MandatoryExtension => WebSocketCloseStatus.MandatoryExtension,
            SocketCloseStatus.MessageTooBig => WebSocketCloseStatus.MessageTooBig,
            SocketCloseStatus.NormalClosure => WebSocketCloseStatus.NormalClosure,
            SocketCloseStatus.PolicyViolation => WebSocketCloseStatus.PolicyViolation,
            SocketCloseStatus.ProtocolError => WebSocketCloseStatus.ProtocolError,
            _ => WebSocketCloseStatus.Empty,
        };

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_activeProtocol is { })
            {
                await _activeProtocol.DisposeAsync().ConfigureAwait(false);
            }

            _socket.Dispose();
            _disposed = true;
        }
    }
}
