using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class MessageReceiver
    {
        private readonly ISocketConnection _connection;
        private readonly PipeWriter _writer;

        public MessageReceiver(
            ISocketConnection connection,
            PipeWriter writer)
        {
            _connection = connection;
            _writer = writer;
        }

        public async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            while (!_connection.Closed
                && !cancellationToken.IsCancellationRequested)
            {
                await _connection
                    .ReceiveAsync(_writer, cancellationToken)
                    .ConfigureAwait(false);

                await WriteMessageDelimiterAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            _writer.Complete();
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
