using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Server;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class MessageProcessor
    {
        private readonly ISocketConnection _connection;
        private readonly PipeReader _reader;
        private readonly IMessagePipeline _pipeline;

        public MessageProcessor(
            ISocketConnection connection,
            IMessagePipeline pipeline,
            PipeReader reader)
        {
            _connection = connection;
            _pipeline = pipeline;
            _reader = reader;
        }

        public void Begin(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(
                () => ProcessMessagesAsync(cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private async Task ProcessMessagesAsync(
            CancellationToken cancellationToken)
        {
            while (true)
            {
                ReadResult result = await _reader
                    .ReadAsync(cancellationToken)
                    .ConfigureAwait(false);

                ReadOnlySequence<byte> buffer = result.Buffer;
                SequencePosition? position = null;

                do
                {
                    position = buffer.PositionOf(Subscription._delimiter);

                    if (position != null)
                    {
                        await _pipeline.ProcessAsync(
                                _connection,
                                buffer.Slice(0, position.Value),
                                cancellationToken)
                            .ConfigureAwait(false);

                        // Skip the message which was read.
                        buffer = buffer
                            .Slice(buffer.GetPosition(1, position.Value));
                    }
                }
                while (position != null);

                _reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            _reader.Complete();
        }
    }
}
