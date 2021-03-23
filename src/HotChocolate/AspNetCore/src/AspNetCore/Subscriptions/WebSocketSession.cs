using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore.Subscriptions
{
    public class WebSocketSession : ISocketSession
    {
        private readonly Pipe _pipe = new Pipe();
        private readonly ISocketConnection _connection;
        private readonly KeepConnectionAliveJob _keepAlive;
        private readonly MessageProcessor _messageProcessor;
        private readonly MessageReceiver _messageReceiver;
        private readonly bool _disposeConnection;
        private bool _disposed;

        private WebSocketSession(
            ISocketConnection connection,
            IMessagePipeline messagePipeline,
            bool disposeConnection)
        {
            _connection = connection;
            _disposeConnection = disposeConnection;

            _keepAlive = new KeepConnectionAliveJob(connection);
            _messageProcessor = new MessageProcessor(connection, messagePipeline, _pipe.Reader);
            _messageReceiver = new MessageReceiver(connection, _pipe.Writer);
        }

        public async Task HandleAsync(CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (await _connection.TryOpenAsync())
            {
                try
                {
                    _keepAlive.Begin(cts.Token);
                    _messageProcessor.Begin(cts.Token);
                    await _messageReceiver.ReceiveAsync(cts.Token);
                }
                catch(OperationCanceledException) 
                { 
                    // OperationCanceledException are catched and will not
                    // bubble further. We will just close the current subscription
                    // context.
                }
                finally
                {
                    cts.Cancel();
                    await _connection.CloseAsync(
                            "Session ended.",
                            SocketCloseStatus.NormalClosure,
                            CancellationToken.None);
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
            if (httpContext is null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (messagePipeline is null)
            {
                throw new ArgumentNullException(nameof(messagePipeline));
            }

            var connection = WebSocketConnection.New(httpContext);
            return new WebSocketSession(connection, messagePipeline, true);
        }
    }
}
