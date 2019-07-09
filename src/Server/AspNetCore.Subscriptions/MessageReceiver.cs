using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class MessageReceiver
    {
        private readonly WebSocketConnection _connection;
        private readonly PipeWriter _writer;
        private readonly CancellationToken _sessionAborted;

        public MessageReceiver(
            WebSocketConnection connection,
            PipeWriter writer,
            CancellationToken sessionAborted)
        {
            _connection = connection;
            _writer = writer;
            _sessionAborted = sessionAborted;
        }

        public async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            using (var combined = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _sessionAborted))
            {
                while (!_connection.Closed || !combined.IsCancellationRequested)
                {
                    await _connection
                        .ReceiveAsync(_writer, combined.Token)
                        .ConfigureAwait(false);

                    await WriteMessageDelimiterAsync(combined.Token)
                        .ConfigureAwait(false);
                }

                _writer.Complete();
            }
        }

        private async Task WriteMessageDelimiterAsync(
            CancellationToken cancellationToken)
        {
            Memory<byte> memory = _writer.GetMemory(1);

            memory.Span[0] = Subscription._delimiter;

            _writer.Advance(1);

            await _writer
                .FlushAsync(cancellationToken)
                .ConfigureAwait(false);

        }
    }
}
