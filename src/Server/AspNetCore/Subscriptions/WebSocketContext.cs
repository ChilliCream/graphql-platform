#if !ASPNETCLASSIC

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class WebSocketContext
        : IWebSocketContext
    {
        private const int _maxMessageSize = 1024 * 4;
        private readonly ConcurrentDictionary<string, ISubscription> _subscriptions =
            new ConcurrentDictionary<string, ISubscription>();
        private readonly OnConnectWebSocketAsync _onConnectAsync;
        private readonly OnCreateRequestAsync _onCreateRequest;
        private bool _disposed;

        public WebSocketContext(
            HttpContext httpContext,
            WebSocket webSocket,
            IQueryExecuter queryExecuter,
            OnConnectWebSocketAsync onConnectAsync,
            OnCreateRequestAsync onCreateRequest)
        {
            HttpContext = httpContext
                ?? throw new ArgumentNullException(nameof(httpContext));
            WebSocket = webSocket
                ?? throw new ArgumentNullException(nameof(webSocket));
            QueryExecuter = queryExecuter
                ?? throw new ArgumentNullException(nameof(queryExecuter));

            _onConnectAsync = onConnectAsync;
            _onCreateRequest = onCreateRequest;
        }

        public HttpContext HttpContext { get; }

        public IQueryExecuter QueryExecuter { get; }

        public WebSocket WebSocket { get; }

        public WebSocketCloseStatus? CloseStatus => WebSocket.CloseStatus;

        public IDictionary<string, object> RequestProperties { get; private set; }

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

            if (_subscriptions.TryRemove(subscriptionId,
                out ISubscription subscription))
            {
                subscription.Dispose();
            }
        }

        public async Task PrepareRequestAsync(QueryRequest request)
        {
            var properties = new Dictionary<string, object>(RequestProperties);
            request.Properties = properties;

            if (_onCreateRequest != null)
            {
                await _onCreateRequest(
                    HttpContext, request, properties,
                    HttpContext.RequestAborted);
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

        public async Task<ConnectionStatus> OpenAsync(
            IDictionary<string, object> properties)
        {
            RequestProperties = properties ?? new Dictionary<string, object>();
            RequestProperties[nameof(ClaimsIdentity)] = HttpContext.User;

            if (_onConnectAsync == null)
            {
                return ConnectionStatus.Accept();
            }

            return await _onConnectAsync(
                HttpContext,
                RequestProperties,
                HttpContext.RequestAborted);
        }

        public async Task CloseAsync()
        {
            if (WebSocket.CloseStatus.HasValue)
            {
                return;
            }

            // TODO : We  have to provide a description and close status here.
            await WebSocket.CloseAsync(
                WebSocketCloseStatus.Empty,
                "closed",
                CancellationToken.None);

            Dispose();
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

#endif
