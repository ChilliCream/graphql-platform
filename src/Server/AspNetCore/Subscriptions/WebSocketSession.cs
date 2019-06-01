#if !ASPNETCLASSIC
using System.IO.Pipelines;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    // TODO : Hanlde close status
    internal sealed class WebSocketSession
        : IDisposable
    {
        private const string _protocol = "graphql-ws";
        private const int _keepAliveTimeout = 5000;

        private readonly Pipe _pipe = new Pipe();
        private readonly SubscriptionReceiver _subscriptionReceiver;
        private readonly SubscriptionReplier _subscriptionReplier;

        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
        private readonly IWebSocketContext _context;

        private WebSocketSession(
            IWebSocketContext context)
        {
            _context = context;
            _subscriptionReplier = new SubscriptionReplier(
                _pipe.Reader, new WebSocketPipeline(context, _cts), _cts);
            _subscriptionReceiver = new SubscriptionReceiver(
                context, _pipe.Writer, _cts);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                StartKeepConnectionAlive();
                _subscriptionReplier.Start(cancellationToken);

                await _subscriptionReceiver
                    .StartAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                 await _context.CloseAsync().ConfigureAwait(false);
            }
        }

        private void StartKeepConnectionAlive()
        {
            Task.Run(KeepConnectionAlive);
        }

        private async Task KeepConnectionAlive()
        {
            while (!_context.CloseStatus.HasValue
                || !_cts.IsCancellationRequested)
            {
                await Task.Delay(_keepAliveTimeout, _cts.Token)
                    .ConfigureAwait(false);
                await _context.SendConnectionKeepAliveMessageAsync(_cts.Token)
                    .ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _context.Dispose();
            _cts.Dispose();
        }

        public static async Task<WebSocketSession> TryCreateAsync(
            HttpContext httpContext,
            IQueryExecutor queryExecutor,
            OnConnectWebSocketAsync onConnectAsync,
            OnCreateRequestAsync onCreateRequest)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (queryExecutor == null)
            {
                throw new ArgumentNullException(nameof(queryExecutor));
            }

            WebSocket socket = await httpContext.WebSockets
                .AcceptWebSocketAsync(_protocol)
                .ConfigureAwait(false);

            if (httpContext.WebSockets.WebSocketRequestedProtocols
                .Contains(socket.SubProtocol))
            {
                var context = new WebSocketContext(
                    httpContext, socket, queryExecutor,
                    onConnectAsync, onCreateRequest);

                return new WebSocketSession(context);
            }
            else
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.ProtocolError,
                    "Expected graphql-ws protocol.",
                    CancellationToken.None)
                    .ConfigureAwait(false);
                socket.Dispose();
            }

            return null;
        }
    }
}

#endif
