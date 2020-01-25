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
        private readonly Pipe _pipe = new Pipe();
        private readonly ISocketConnection _connection;
        private readonly KeepConnectionAliveJob _keepAlive;
        private readonly MessageProcessor _messageProcessor;
        private readonly MessageReceiver _messageReciver;
        private readonly bool _disposeConnection;
        private bool _disposed;

        public WebSocketSession(
            ISocketConnection connection,
            IMessagePipeline messagePipeline,
            bool disposeConnection)
        {
            _connection = connection;
            _disposeConnection = disposeConnection;

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
            using (var cts = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken))
            {
                if (await _connection.TryOpenAsync().ConfigureAwait(false))
                {
                    try
                    {
                        _keepAlive.Begin(cts.Token);
                        _messageProcessor.Begin(cts.Token);
                        await _messageReciver.ReceiveAsync(cts.Token)
                            .ConfigureAwait(false);
                    }
                    finally
                    {
                        cts.Cancel();
                        await _connection.CloseAsync(
                            "Session ended.",
                            SocketCloseStatus.NormalClosure,
                            CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing && _disposeConnection)
                {
                    _connection.Dispose();
                }
                _disposed = true;
            }
        }

        public static WebSocketSession New(
            HttpContext httpContext,
            IMessagePipeline messagePipeline)
        {
            var connection = WebSocketConnection.New(httpContext);
            return new WebSocketSession(connection, messagePipeline, true);
        }
    }
}
