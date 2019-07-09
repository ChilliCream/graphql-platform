using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Subscriptions.Messages;
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
        private readonly MessageProcessor _messageProcessor;
        private readonly MessageReceiver _messageReciver;

        public WebSocketSession(
            WebSocketConnection connection,
            IMessagePipeline messagePipeline)
        {
            _connection = connection;

            _keepAlive = new WebSocketKeepAlive(
                context,
                KeepAliveTimeout,
                _cts);

            _messageProcessor = new MessageProcessor(
                connection,
                _pipe.Reader,
                messagePipeline);

            _messageReciver = new MessageReceiver(
                context, _pipe.Writer, _cts);
        }

        public async Task HandleAsync(CancellationToken cancellationToken)
        {
            if (await _connection.TryOpenAsync().ConfigureAwait(false))
            {
                try
                {
                    _messageProcessor.Begin(cancellationToken);
                    await _messageReciver.ReceiveAsync(cancellationToken)
                        .ConfigureAwait(false);
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
