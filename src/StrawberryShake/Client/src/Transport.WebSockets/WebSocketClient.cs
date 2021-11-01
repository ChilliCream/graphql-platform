using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Properties;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Represents a client for sending and receiving messages responses over a websocket
    /// identified by a URI and name.
    /// </summary>
    public sealed class WebSocketClient
        : IWebSocketClient
    {
        private readonly IReadOnlyList<ISocketProtocolFactory> _protocolFactories;
        private readonly ClientWebSocket _socket;
        private readonly string _name;

        private const int _maxMessageSize = 1024 * 4;

        private ISocketProtocol? _activeProtocol;
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
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _protocolFactories = protocolFactories ??
                throw new ArgumentNullException(nameof(protocolFactories));
            _socket = new ClientWebSocket();

            for (var i = 0; i < _protocolFactories.Count; i++)
            {
                _socket.Options.AddSubProtocol(_protocolFactories[i].ProtocolName);
            }
        }

        /// <inheritdoc />
        public Uri? Uri { get; set; }

        /// <inheritdoc />
        public ISocketConnectionInterceptor ConnectionInterceptor { get; set; } =
            DefaultSocketConnectionInterceptor.Instance;

        /// <inheritdoc />
        public string Name => _name;

        /// <inheritdoc />
        public bool IsClosed =>
            _disposed
            || _socket.CloseStatus.HasValue 
            || _socket.State == WebSocketState.Aborted;

        /// <inheritdoc />
        public ClientWebSocket Socket => _socket;

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

            if (MemoryMarshal.TryGetArray(message, out ArraySegment<byte> buffer))
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
                    Memory<byte> memory = writer.GetMemory(_maxMessageSize);
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
        {
            switch (closeStatus)
            {
                case SocketCloseStatus.EndpointUnavailable:
                    return WebSocketCloseStatus.EndpointUnavailable;
                case SocketCloseStatus.InternalServerError:
                    return WebSocketCloseStatus.InternalServerError;
                case SocketCloseStatus.InvalidMessageType:
                    return WebSocketCloseStatus.InvalidMessageType;
                case SocketCloseStatus.InvalidPayloadData:
                    return WebSocketCloseStatus.InvalidPayloadData;
                case SocketCloseStatus.MandatoryExtension:
                    return WebSocketCloseStatus.MandatoryExtension;
                case SocketCloseStatus.MessageTooBig:
                    return WebSocketCloseStatus.MessageTooBig;
                case SocketCloseStatus.NormalClosure:
                    return WebSocketCloseStatus.NormalClosure;
                case SocketCloseStatus.PolicyViolation:
                    return WebSocketCloseStatus.PolicyViolation;
                case SocketCloseStatus.ProtocolError:
                    return WebSocketCloseStatus.ProtocolError;
                default:
                    return WebSocketCloseStatus.Empty;
            }
        }

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
}
