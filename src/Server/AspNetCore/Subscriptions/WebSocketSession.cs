#if !ASPNETCLASSIC

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

        private readonly static IRequestHandler[] _requestHandlers =
            new IRequestHandler[]
            {
                new ConnectionInitializeHandler(),
                new ConnectionTerminateHandler(),
                new SubscriptionStartHandler(),
                new SubscriptionStopHandler(),
            };

        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();
        private readonly IWebSocketContext _context;

        private WebSocketSession(
            IWebSocketContext context)
        {
            _context = context;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                StartKeepConnectionAlive();
                await ReceiveMessagesAsync(cancellationToken);
            }
            finally
            {
                 await _context.CloseAsync();
            }
        }

        private async Task ReceiveMessagesAsync(
            CancellationToken cancellationToken)
        {
            using (var combined = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _cts.Token))
            {
                while (!_context.CloseStatus.HasValue
                    || !combined.IsCancellationRequested)
                {
                    GenericOperationMessage message = await _context
                        .ReceiveMessageAsync(combined.Token);

                    if (message == null)
                    {
                        await _context.SendConnectionKeepAliveMessageAsync(
                            combined.Token);
                    }
                    else
                    {
                        await HandleMessage(message, combined.Token);
                    }
                }
            }
        }

        private Task HandleMessage(
            GenericOperationMessage message,
            CancellationToken cancellationToken)
        {
            foreach (IRequestHandler requestHandler in _requestHandlers)
            {
                if (requestHandler.CanHandle(message))
                {
                    return requestHandler.HandleAsync(
                        _context,
                        message,
                        cancellationToken);
                }
            }

            throw new NotSupportedException(
                "The specified message type is not supported.");
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
                await Task.Delay(_keepAliveTimeout, _cts.Token);
                await _context.SendConnectionKeepAliveMessageAsync(_cts.Token);
            }
        }

        public void Dispose()
        {
            _context.Dispose();
            _cts.Dispose();
        }

        public static async Task<WebSocketSession> TryCreateAsync(
            HttpContext httpContext,
            IQueryExecuter queryExecuter,
            OnConnectWebSocketAsync onConnectAsync,
            OnCreateRequestAsync onCreateRequest)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (queryExecuter == null)
            {
                throw new ArgumentNullException(nameof(queryExecuter));
            }

            WebSocket socket = await httpContext.WebSockets
                .AcceptWebSocketAsync(_protocol);

            if (httpContext.WebSockets.WebSocketRequestedProtocols
                .Contains(socket.SubProtocol))
            {
                var context = new WebSocketContext(
                    httpContext, socket, queryExecuter,
                    onConnectAsync, onCreateRequest);

                return new WebSocketSession(context);
            }
            else
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.ProtocolError,
                    "Expected graphql-ws protocol.",
                    CancellationToken.None);
                socket.Dispose();
            }

            return null;
        }
    }
}

#endif
