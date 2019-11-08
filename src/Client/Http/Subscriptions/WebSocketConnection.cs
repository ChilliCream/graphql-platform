using System;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Subscriptions
{
    public sealed class WebSocketConnection
        : ISocketConnection
    {
        private const string _protocol = "graphql-ws";
        private const int _maxMessageSize = 1024 * 4;
        private ClientWebSocket? _webSocket;
        private bool _disposed;

        public event EventHandler? Disposed;

        public WebSocketConnection(Uri uri)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public Uri Uri { get; }

        public bool IsClosed =>
            _disposed
            || _webSocket == null
            || _webSocket.CloseStatus.HasValue;

        public async Task OpenAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebSocketConnection));
            }

            // TODO : we need a factory for the socket that can be customized.
            _webSocket = new ClientWebSocket();
            _webSocket.Options.AddSubProtocol(_protocol);

            // TODO : should we introduce a abstract exception here if we cannot connect?
            await _webSocket.ConnectAsync(Uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task CloseAsync(
            string message,
            SocketCloseStatus closeStatus,
            CancellationToken cancellationToken = default)
        {
            try
            {
                WebSocket? webSocket = _webSocket;

                if (IsClosed || webSocket is null)
                {
                    return;
                }

                await webSocket.CloseOutputAsync(
                        MapCloseStatus(closeStatus),
                        message,
                        cancellationToken)
                    .ConfigureAwait(false);

                Dispose();
            }
            catch
            {
                // we do not throw here ...
            }
        }

        public Task SendAsync(
            ReadOnlyMemory<byte> message,
            CancellationToken cancellationToken = default)
        {
            WebSocket? webSocket = _webSocket;

            if (IsClosed || webSocket is null)
            {
                return Task.CompletedTask;
            }

            if (MemoryMarshal.TryGetArray(message, out ArraySegment<byte> buffer))
            {
                return webSocket.SendAsync(
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
            WebSocket? webSocket = _webSocket;

            if (IsClosed || webSocket is null)
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
                            socketResult = await webSocket
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

        private static WebSocketCloseStatus MapCloseStatus(
            SocketCloseStatus closeStatus)
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _webSocket?.Dispose();
                _webSocket = null;
                _disposed = true;

                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
