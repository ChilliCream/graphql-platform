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
            IQueryExecutor queryExecutor,
            OnConnectWebSocketAsync onConnectAsync,
            OnCreateRequestAsync onCreateRequest)
        {
            HttpContext = httpContext
                ?? throw new ArgumentNullException(nameof(httpContext));
            WebSocket = webSocket
                ?? throw new ArgumentNullException(nameof(webSocket));
            QueryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));

            _onConnectAsync = onConnectAsync;
            _onCreateRequest = onCreateRequest;
        }

        public HttpContext HttpContext { get; }

        public IQueryExecutor QueryExecutor { get; }

        public WebSocket WebSocket { get; }

        public WebSocketCloseStatus? CloseStatus => WebSocket.CloseStatus;

        public IDictionary<string, object> RequestProperties
        { get; private set; }

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

        public async Task PrepareRequestAsync(IQueryRequestBuilder requestBuilder)
        {
            requestBuilder.SetProperties(
                new Dictionary<string, object>(RequestProperties));

            if (_onCreateRequest != null)
            {
                await _onCreateRequest(
                    HttpContext, requestBuilder,
                    HttpContext.RequestAborted)
                    .ConfigureAwait(false);
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
                    isEOF, cancellationToken)
                    .ConfigureAwait(false);
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
                    cancellationToken)
                    .ConfigureAwait(false);

                await messageStream.WriteAsync(
                    buffer, 0, result.Count,
                    cancellationToken)
                    .ConfigureAwait(false);
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
                HttpContext.RequestAborted)
                .ConfigureAwait(false);
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
                CancellationToken.None)
                .ConfigureAwait(false);

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
