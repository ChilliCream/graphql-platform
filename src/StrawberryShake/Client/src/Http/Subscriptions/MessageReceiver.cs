using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport;

namespace StrawberryShake.Http.Subscriptions
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

        public Task? InnerTask { get; private set; }

        public void Start(CancellationToken cancellationToken)
        {
            if (InnerTask is { })
            {
                return;
            }

            InnerTask = Task.Factory.StartNew(
                () => ReceiveAsync(cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!_connection.IsClosed
                    && !cancellationToken.IsCancellationRequested)
                {
                    await _connection
                        .ReceiveAsync(_writer, cancellationToken)
                        .ConfigureAwait(false);

                    await WriteMessageDelimiterAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                _writer.Complete();
            }
        }

        private async Task WriteMessageDelimiterAsync(
            CancellationToken cancellationToken)
        {
            Memory<byte> memory = _writer.GetMemory(1);

            memory.Span[0] = MessageProcessor.Delimiter;

            _writer.Advance(1);

            await _writer
                .FlushAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
