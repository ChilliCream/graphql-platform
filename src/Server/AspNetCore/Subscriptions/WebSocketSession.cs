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
        private static readonly TimeSpan KeepAliveTimeout =
            TimeSpan.FromSeconds(5);
        private const string _protocol = "graphql-ws";

        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
        private readonly IWebSocketContext _context;

        private readonly Pipe _pipe = new Pipe();
        private readonly WebSocketKeepAlive _keepAlive;
        private readonly SubscriptionReceiver _subscriptionReceiver;
        private readonly SubscriptionReplier _subscriptionReplier;

        private WebSocketSession(
            IWebSocketContext context)
        {
            _context = context;
            _keepAlive = new WebSocketKeepAlive(context, KeepAliveTimeout, _cts);
            _subscriptionReplier = new SubscriptionReplier(
                _pipe.Reader, new WebSocketPipeline(context, _cts), _cts);
            _subscriptionReceiver = new SubscriptionReceiver(
                context, _pipe.Writer, _cts);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _keepAlive.Start();
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
                    httpContext,
                    new WebSocketWrapper(socket),
                    queryExecutor,
                    onConnectAsync,
                    onCreateRequest);

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
