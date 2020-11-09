using System;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class WebSocketConnection
        : ISocketConnection
    {
        private const string _protocol = "graphql-ws";
        private const int _maxMessageSize = 1024 * 4;
        private WebSocket? _webSocket;
        private bool _disposed;

        private WebSocketConnection(HttpContext httpContext)
        {
            HttpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            Subscriptions = new SubscriptionManager(this);
        }

        public bool Closed => _webSocket is null || _webSocket.CloseStatus.HasValue;

        public HttpContext HttpContext { get; }

        public ISubscriptionManager Subscriptions { get; }

        public WebSocketManager WebSockets => HttpContext.WebSockets;

        public IServiceProvider RequestServices => HttpContext.RequestServices;

        public CancellationToken RequestAborted => HttpContext.RequestAborted;

        public async Task<bool> TryOpenAsync()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebSocketConnection));
            }

            _webSocket = await WebSockets.AcceptWebSocketAsync(_protocol);

            if (_webSocket.SubProtocol is not null &&
                WebSockets.WebSocketRequestedProtocols.Contains(_webSocket.SubProtocol))
            {
                return true;
            }

            await _webSocket.CloseOutputAsync(
                WebSocketCloseStatus.ProtocolError,
                "Expected graphql-ws protocol.",
                CancellationToken.None);
            _webSocket.Dispose();
            _webSocket = null;
            return false;
        }

        public Task SendAsync(
            byte[] message,
            CancellationToken cancellationToken)
        {
            WebSocket? webSocket = _webSocket;

            if (_disposed || webSocket == null)
            {
                return Task.CompletedTask;
            }

            return webSocket.SendAsync(
                new ArraySegment<byte>(message),
                WebSocketMessageType.Text,
                true, cancellationToken);
        }

        public async Task ReceiveAsync(
            PipeWriter writer,
            CancellationToken cancellationToken)
        {
            WebSocket? webSocket = _webSocket;

            if (_disposed || webSocket == null)
            {
                return;
            }

            try
            {
                WebSocketReceiveResult? socketResult = null;
                do
                {
                    Memory<byte> memory = writer.GetMemory(_maxMessageSize);
                    var success = MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> buffer);

                    if (success)
                    {
                        try
                        {
                            socketResult = await webSocket.ReceiveAsync(buffer, cancellationToken);
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

                        FlushResult result = await writer.FlushAsync(cancellationToken);
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

        public async Task CloseAsync(
           string message,
           SocketCloseStatus closeStatus,
           CancellationToken cancellationToken)
        {
            try
            {
                WebSocket? webSocket = _webSocket;

                if (_disposed || Closed || webSocket is null)
                {
                    return;
                }

                await webSocket.CloseOutputAsync(
                    MapCloseStatus(closeStatus),
                    message,
                    cancellationToken);

                Dispose();
            }
            catch
            {
                // we do not throw here ...
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Subscriptions.Dispose();
                    _webSocket?.Dispose();
                    _webSocket = null;
                }
                _disposed = true;
            }
        }

        public static WebSocketConnection New(HttpContext httpContext) =>
            new WebSocketConnection(httpContext);
    }
}
