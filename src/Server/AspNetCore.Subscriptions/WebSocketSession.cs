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
        private readonly ISocketConnection _connection;
        private readonly Pipe _pipe = new Pipe();
        private readonly KeepConnectionAliveJob _keepAlive;
        private readonly MessageProcessor _messageProcessor;
        private readonly MessageReceiver _messageReciver;

        public WebSocketSession(
            ISocketConnection connection,
            IMessagePipeline messagePipeline)
        {
            _connection = connection;

            _keepAlive = new KeepConnectionAliveJob(
                connection);

            _messageProcessor = new MessageProcessor(
                connection,
                messagePipeline,
                _pipe.Reader);

            _messageReciver = new MessageReceiver(
                connection,
                _pipe.Writer);
        }

        public async Task HandleAsync(CancellationToken cancellationToken)
        {
            if (await _connection.TryOpenAsync().ConfigureAwait(false))
            {
                try
                {
                    _keepAlive.Begin(cancellationToken);
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

        public static WebSocketSession New(
            HttpContext httpContext,
            IMessagePipeline messagePipeline)
        {
            var connection = WebSocketConnection.New(httpContext);
            return new WebSocketSession(connection, messagePipeline);
        }
    }


}
