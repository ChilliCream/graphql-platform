using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Sockets;
using StrawberryShake.Transport.WebSockets;

#nullable enable

namespace HotChocolate.Stitching.Integration;

public delegate Task<WebSocket> ConnectDelegate(Uri uri, CancellationToken cancellationToken);

public sealed class TestWebSocketClient : IWebSocketClient
{
    private readonly IReadOnlyList<ISocketProtocolFactory> _protocolFactories;
    private readonly ConnectDelegate _connect;
    private WebSocket? _socket;
    private ISocketProtocol? _activeProtocol;
    private bool _disposed;

    /// <summary>
    /// Creates a new instance of <see cref="WebSocketClient"/>
    /// </summary>
    /// <param name="name">The name of the socket</param>
    /// <param name="protocolFactories">The protocol factories this socket supports</param>
    /// <param name="connect"></param>
    public TestWebSocketClient(
        string name,
        IReadOnlyList<ISocketProtocolFactory> protocolFactories,
        ConnectDelegate connect)
    {
        Name = name ??
            throw new ArgumentNullException(nameof(name));
        _protocolFactories = protocolFactories ??
            throw new ArgumentNullException(nameof(protocolFactories));
        _connect = connect;
    }

    /// <inheritdoc />
    public Uri? Uri { get; set; } = new("ws://localhost:5000");

    /// <inheritdoc />
    public ISocketConnectionInterceptor ConnectionInterceptor { get; set; } =
        DefaultSocketConnectionInterceptor.Instance;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public bool IsClosed
    {
        get => _disposed || _socket.IsClosed();
    }

    /// <inheritdoc />
    public WebSocket Socket => _socket!;

    /// <inheritdoc />
    public async Task<ISocketProtocol> OpenAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WebSocketClient));
        }

        if (Uri is null)
        {
            throw new InvalidOperationException("Uri not set.");
        }

        _socket = await _connect(Uri, cancellationToken);

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
                "Failed to initialize protocol",
                SocketCloseStatus.ProtocolError,
                cancellationToken)
                .ConfigureAwait(false);

            throw new SocketOperationException(_socket.SubProtocol ?? "null");
        }

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
                    await _activeProtocol.TerminateAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // In case termination of the protocol failed we still want to close the
                    // socket
                }
            }

            await _socket!.CloseOutputAsync(
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

        if (MemoryMarshal.TryGetArray(message, out ArraySegment<byte> buffer))
        {
            await _socket!.SendAsync(
                buffer,
                WebSocketMessageType.Text,
                true,
                cancellationToken)
                .ConfigureAwait(false);
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
                Memory<byte> memory = writer.GetMemory(SocketDefaults.BufferSize);
                try
                {
                    if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> buffer))
                    {
                        socketResult = await _socket!
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

                FlushResult result = await writer
                    .FlushAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (result.IsCompleted)
                {
                    break;
                }
            } while (socketResult == null || !socketResult.EndOfMessage);
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
            _ => WebSocketCloseStatus.Empty
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

            _socket?.Dispose();
            _disposed = true;
        }
    }
}
