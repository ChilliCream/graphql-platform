using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.WebSockets;
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
        : ISocketClient
    {
        private readonly IReadOnlyList<ISocketProtocolFactory> _protocolFactories;
        private readonly ClientWebSocket _socket;

        private const int _maxMessageSize = 1024 * 4;

        private ISocketProtocol? _activeProtocol;
        private bool _disposed;
        private readonly string _name;

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
        public string Name => _name;

        /// <inheritdoc />
        public bool IsClosed =>
            _disposed
            || _socket.CloseStatus.HasValue;

        /// <inheritdoc />
        public async Task OpenAsync(CancellationToken cancellationToken = default)
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
        public bool TryGetProtocol([NotNullWhen(true)] out ISocketProtocol? protocol )
        {
            if (!IsClosed && _activeProtocol is not null)
            {
                protocol = _activeProtocol;
                return true;
            }

            protocol = null;
            return false;
        }

        /// <inheritdoc />
        public ValueTask SendAsync(
            ReadOnlyMemory<byte> message,
            CancellationToken cancellationToken = default)
        {
            if (IsClosed)
            {
                return default;
            }

            return _socket.SendAsync(
                message,
                WebSocketMessageType.Text,
                true,
                cancellationToken);
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
                ValueWebSocketReceiveResult? socketResult;
                do
                {
                    Memory<byte> memory = writer.GetMemory(_maxMessageSize);
                    try
                    {
                        socketResult = await _socket
                            .ReceiveAsync(memory, cancellationToken)
                            .ConfigureAwait(false);

                        if (socketResult.Value.Count == 0)
                        {
                            break;
                        }

                        writer.Advance(socketResult.Value.Count);
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
                } while (!socketResult.Value.EndOfMessage);
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
                _socket.Dispose();
                if (_activeProtocol is { })
                {
                    await _activeProtocol.DisposeAsync().ConfigureAwait(false);
                }

                _disposed = true;
            }
        }
    }
}
