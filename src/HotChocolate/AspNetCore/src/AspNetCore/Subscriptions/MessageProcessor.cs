using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

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

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    SequencePosition? position;
                    ReadResult result = await _reader.ReadAsync(cancellationToken);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    do
                    {
                        position = buffer.PositionOf(SubscriptionSession.Delimiter);

                        if (position is not null)
                        {
                            await _pipeline.ProcessAsync(
                                _connection,
                                buffer.Slice(0, position.Value),
                                cancellationToken);

                            // Skip the message which was read.
                            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                        }
                    }
                    while (position != null);

                    _reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch(OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            finally
            {
                // reader should be completed always, so that related pipe writer can
                // stop write new messages
                await _reader.CompleteAsync();
            }
        }
    }
}
