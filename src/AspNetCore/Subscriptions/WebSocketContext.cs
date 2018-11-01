using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class WebSocketContext
        : IWebSocketContext
    {
        private const int _maxMessageSize = 1024 * 4;
        private readonly ConcurrentDictionary<string, ISubscription> _subscriptions =
            new ConcurrentDictionary<string, ISubscription>();
        private bool _disposed;

        public WebSocketContext(
            HttpContext httpContext,
            WebSocket webSocket,
            QueryExecuter queryExecuter)
        {
            HttpContext = httpContext
                ?? throw new ArgumentNullException(nameof(httpContext));
            WebSocket = webSocket
                ?? throw new ArgumentNullException(nameof(webSocket));
            QueryExecuter = queryExecuter
                ?? throw new ArgumentNullException(nameof(queryExecuter));
        }

        public HttpContext HttpContext { get; }

        public QueryExecuter QueryExecuter { get; }

        public WebSocket WebSocket { get; }

        public WebSocketCloseStatus? CloseStatus => WebSocket.CloseStatus;

        public void RegisterSubscription(ISubscription subscription)
        {
            if (subscription == null)
            {
                throw new ArgumentNullException(nameof(subscription));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException("WebSocketContext");
            }

            if (_subscriptions.TryAdd(subscription.Id, subscription))
            {
                subscription.Completed += (sender, eventArgs) =>
                {
                    UnregisterSubscription(subscription.Id);
                };
            }
        }

        public void UnregisterSubscription(string subscriptionId)
        {
            if (subscriptionId == null)
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }

            if (_disposed)
            {
                throw new ObjectDisposedException("WebSocketContext");
            }

            if (_subscriptions.TryRemove(subscriptionId, out var subscription))
            {
                subscription.Dispose();
            }
        }

        public async Task SendMessageAsync(
            Stream messageStream,
            CancellationToken cancellationToken)
        {
            var read = 0;
            var buffer = new byte[_maxMessageSize];

            do
            {
                read = messageStream.Read(buffer, 0, buffer.Length);
                var segment = new ArraySegment<byte>(buffer, 0, read);
                var isEOF = messageStream.Position == messageStream.Length;

                await WebSocket.SendAsync(
                    segment, WebSocketMessageType.Text,
                    isEOF, cancellationToken);
            } while (read == _maxMessageSize);
        }

        public async Task ReceiveMessageAsync(
            Stream messageStream,
            CancellationToken cancellationToken)
        {
            WebSocketReceiveResult result;
            var buffer = new byte[_maxMessageSize];

            do
            {
                result = await WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                await messageStream.WriteAsync(
                    buffer, 0, result.Count,
                    cancellationToken);
            }
            while (!result.EndOfMessage);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                WebSocket.Dispose();

                foreach (ISubscription subscription in _subscriptions.Values)
                {
                    subscription.Dispose();
                }

                _subscriptions.Clear();
            }
        }
    }
}
