using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore.Subscriptions
{
    // TODO : Keep Alive
    // TODO : Hanlde close status
    public class WebSocketSession
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
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }
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
            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(_keepAliveTimeout, _cts.Token);
                await _context.SendConnectionKeepAliveMessageAsync(_cts.Token);
            }
        }

        public void Dispose()
        {

        }

        public static async Task<WebSocketSession> TryCreateAsync(
            HttpContext httpContext,
            QueryExecuter queryExecuter)
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
                    httpContext, socket, queryExecuter);
                return new WebSocketSession(context);
            }
            else
            {
                // TODO : send error message
                socket.Dispose();
            }

            return null;
        }
    }
}
