using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class MessageReceiver
    {
        private readonly ISocketConnection _connection;
        private readonly PipeWriter _writer;

        public MessageReceiver(ISocketConnection connection, PipeWriter writer)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            while (!_connection.Closed &&
                !cancellationToken.IsCancellationRequested)
            {
                await _connection.ReceiveAsync(_writer, cancellationToken);
                await WriteMessageDelimiterAsync(cancellationToken);
            }
            await _writer.CompleteAsync();
        }

        private async Task WriteMessageDelimiterAsync(
            CancellationToken cancellationToken)
        {
            Memory<byte> memory = _writer.GetMemory(1);
            memory.Span[0] = Subscription.Delimiter;
            _writer.Advance(1);
            await _writer.FlushAsync(cancellationToken);
        }
    }
}
