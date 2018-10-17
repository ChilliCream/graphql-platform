using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class WebSocketContext
        : IWebSocketContext
    {
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
