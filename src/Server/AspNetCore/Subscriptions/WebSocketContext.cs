#if !ASPNETCLASSIC
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal class WebSocketContext
        : IWebSocketContext
    {
        private readonly ConcurrentDictionary<string, ISubscription> _subscriptions =
            new ConcurrentDictionary<string, ISubscription>();
        private readonly OnConnectWebSocketAsync _onConnectAsync;
        private readonly OnCreateRequestAsync _onCreateRequest;
        private readonly IWebSocket _webSocket;

        private bool _disposed;

        public WebSocketContext(
            IHttpContext httpContext,
            IWebSocket webSocket,
            IQueryExecutor queryExecutor,
            OnConnectWebSocketAsync onConnectAsync,
            OnCreateRequestAsync onCreateRequest)
        {
            HttpContext = httpContext
                ?? throw new ArgumentNullException(nameof(httpContext));
            _webSocket = webSocket
                ?? throw new ArgumentNullException(nameof(webSocket));
            QueryExecutor = queryExecutor
                ?? throw new ArgumentNullException(nameof(queryExecutor));

            _onConnectAsync = onConnectAsync;
            _onCreateRequest = onCreateRequest;
        }

        public IHttpContext HttpContext { get; }

        public IQueryExecutor QueryExecutor { get; }

        public bool Closed => _webSocket.Closed;

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

        public async Task PrepareRequestAsync(
            IQueryRequestBuilder requestBuilder)
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
            await _webSocket
                .SendAsync(messageStream, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task ReceiveMessageAsync(
            PipeWriter writer,
            CancellationToken cancellationToken)
        {
            await _webSocket
                .ReceiveAsync(writer, cancellationToken)
                .ConfigureAwait(false);
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
            // TODO : We  have to provide a description and close status here.
            await _webSocket.CloseAsync(
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
                _webSocket.Dispose();

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
