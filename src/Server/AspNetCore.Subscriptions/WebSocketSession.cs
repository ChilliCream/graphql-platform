using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Server;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class WebSocketSession
        : ISocketSession
    {
        private static readonly TimeSpan KeepAliveTimeout =
            TimeSpan.FromSeconds(5);
        private readonly WebSocketConnection _connection;


        private readonly CancellationTokenSource _cts =
            new CancellationTokenSource();

        private readonly Pipe _pipe = new Pipe();
        private readonly WebSocketKeepAlive _keepAlive;
        private readonly MessageReceiver _subscriptionReceiver;
        private readonly MessageReplier _subscriptionReplier;

        public WebSocketSession(WebSocketConnection connection)
        {
            _connection = connection
                ?? throw new ArgumentNullException(nameof(connection));
            _keepAlive = new WebSocketKeepAlive(context, KeepAliveTimeout, _cts);
            _subscriptionReplier = new MessageReplier(
                _pipe.Reader, new MessagePipeline(context, _cts), _cts);
            _subscriptionReceiver = new MessageReceiver(
                context, _pipe.Writer, _cts);
        }

        public async Task HandleAsync(CancellationToken cancellationToken)
        {
            if (await _connection.TryOpenAsync().ConfigureAwait(false))
            {
                try
                {
                    // _connection.
                    _subscriptionReplier.Start(cancellationToken);
                }
                finally
                {
                    await _connection.CloseAsync(
                        "Session ended.",
                        cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public WebSocketSession New(HttpContext httpContext)
        {

        }
    }


}
