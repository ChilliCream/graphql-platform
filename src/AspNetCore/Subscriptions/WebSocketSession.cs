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

        private readonly static IRequestHandler[] _requestHandlers =
            new IRequestHandler[]
            {
                new ConnectionInitializeHandler(),
                new ConnectionTerminateHandler(),
    };
        private static readonly IRequestHandler _unknownRequestHandler;

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
            while (!_context.WebSocket.CloseStatus.HasValue)
            {
                GenericOperationMessage message = await _context
                    .ReceiveMessageAsync(cancellationToken);

                if (message == null)
                {
                    await _context.SendConnectionKeepAliveMessageAsync(
                        cancellationToken);
                }
                else
                {
                    await HandleMessage(message, cancellationToken);
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

            throw new NotSupportedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
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

            if (socket.SubProtocol.Contains(_protocol))
            {
                var context = new WebSocketContext(
                    httpContext, socket, queryExecuter);
                return new WebSocketSession(context);
            }

            return null;
        }
    }
}
