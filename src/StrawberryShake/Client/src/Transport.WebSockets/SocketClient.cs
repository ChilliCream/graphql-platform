using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Http.Subscriptions.Messages;

namespace StrawberryShake.Transport.WebSockets
{
    public sealed class WebSocketClient
        : ISocketClient
    {
        private readonly IReadOnlyList<ISocketProtocol> _protocols;
        private readonly ClientWebSocket _socket;

        private const int _maxMessageSize = 1024 * 4;

        private ISocketProtocol? _activeProtocol;
        private bool _disposed;
        private readonly string _name;

        public WebSocketClient(string name, IReadOnlyList<ISocketProtocol> protocols)
        {
            _protocols = protocols;
            _name = name;
            _socket = new ClientWebSocket();

            for (var i = 0; i < _protocols.Count; i++)
            {
                _socket.Options.AddSubProtocol(_protocols[i].ProtocolName);
            }
        }

        public Uri? Uri { get; set; }

        public string? Name => _name;

        public bool IsClosed =>
            _disposed
            || _socket.CloseStatus.HasValue;

        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebSocketClient));
            }

            if (Uri is null)
            {
                // TODO: Uri should not be null
                throw new InvalidOperationException();
            }

            await _socket.ConnectAsync(Uri, cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < _protocols.Count; i++)
            {
                if (_protocols[i].ProtocolName == _socket.SubProtocol)
                {
                    _activeProtocol = _protocols[i];
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

                // TODO throw error
                throw new InvalidOperationException();
            }

            await _activeProtocol.InitializeAsync(this, cancellationToken).ConfigureAwait(false);
        }

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
                    await _activeProtocol.TerminateAsync(cancellationToken).ConfigureAwait(false);
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

        public ISocketProtocol GetProtocol()
        {
            if (_activeProtocol is null)
            {
                // TODO: Connection not established
                throw new InvalidOperationException();
            }

            return _activeProtocol;
        }

        public Task SendAsync(
            ReadOnlyMemory<byte> message,
            CancellationToken cancellationToken = default)
        {
            if (IsClosed)
            {
                return Task.CompletedTask;
            }

            if (MemoryMarshal.TryGetArray(message, out ArraySegment<byte> buffer))
            {
                return _socket.SendAsync(
                    buffer,
                    WebSocketMessageType.Text,
                    true,
                    cancellationToken);
            }

            return Task.CompletedTask;
        }

        public async Task ReceiveAsync(
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
                    if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> buffer))
                    {
                        try
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

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _socket.Dispose();
                foreach (var protocol in _protocols)
                {
                    await protocol.DisposeAsync();
                }

                _disposed = true;
            }
        }
    }
}
