using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Server;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class WebSocketConnection
        : ISocketConnection
    {
        private const string _protocol = "graphql-ws";
        private const int _maxMessageSize = 1024 * 4;
        private WebSocket _webSocket;
        private bool _disposed;

        public WebSocketConnection(HttpContext httpContext)
        {
            HttpContext = httpContext
                ?? throw new ArgumentNullException(nameof(httpContext));

            Subscriptions = new WebSocketSubscriptionManager(this);
        }

        public bool Closed =>
            _webSocket == null
            || _webSocket.CloseStatus.HasValue;

        public HttpContext HttpContext { get; }

        public ISubscriptionManager Subscriptions { get; }

        public IServiceProvider RequestServices => HttpContext.RequestServices;

        public async Task<bool> TryOpenAsync()
        {
            _webSocket = await HttpContext.WebSockets
                .AcceptWebSocketAsync(_protocol)
                .ConfigureAwait(false);

            if (HttpContext.WebSockets.WebSocketRequestedProtocols
                .Contains(_webSocket.SubProtocol))
            {
                return true;
            }

            await _webSocket.CloseAsync(
                WebSocketCloseStatus.ProtocolError,
                "Expected graphql-ws protocol.",
                CancellationToken.None)
                .ConfigureAwait(false);
            _webSocket.Dispose();
            _webSocket = null;
            return false;
        }

        public Task SendAsync(
            byte[] message,
            CancellationToken cancellationToken)
        {
            return _webSocket.SendAsync(
                new ArraySegment<byte>(message),
                WebSocketMessageType.Text,
                true, cancellationToken);
        }

        public async Task ReceiveAsync(
            PipeWriter writer,
            CancellationToken cancellationToken)
        {
            WebSocketReceiveResult socketResult = null;
            do
            {
                Memory<byte> memory = writer.GetMemory(_maxMessageSize);
                bool success = MemoryMarshal.TryGetArray(
                    memory, out ArraySegment<byte> buffer);
                if (success)
                {
                    try
                    {
                        socketResult = await _webSocket
                            .ReceiveAsync(buffer, cancellationToken)
                            .ConfigureAwait(false);

                        if (socketResult.Count == 0)
                        {
                            break;
                        }

                        writer.Advance(socketResult.Count);
                    }
                    catch (Exception)
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

        public async Task CloseAsync(
           string message,
           CancellationToken cancellationToken)
        {
            if (_webSocket.CloseStatus.HasValue)
            {
                return;
            }

            await _webSocket.CloseAsync(
                    WebSocketCloseStatus.Empty,
                    message,
                    cancellationToken)
                .ConfigureAwait(false);

            Dispose();
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
                }
                _disposed = true;
            }
        }

        public static WebSocketConnection New(HttpContext httpContext) =>
            new WebSocketConnection(httpContext);
    }
}
