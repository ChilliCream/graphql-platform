#if !ASPNETCLASSIC
using System;   
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.AspNetCore.Subscriptions
{
    internal sealed class SubscriptionReplier
    {
        private readonly PipeReader _reader;
        private readonly WebSocketPipeline _pipeline;
        private readonly CancellationTokenSource _cts;

        public SubscriptionReplier(
            PipeReader reader,
            WebSocketPipeline pipeline,
            CancellationTokenSource cts)
        {
            _reader = reader;
            _pipeline = pipeline;
            _cts = cts;
        }

        public void Start(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(
                () => StartReplyAsync(cancellationToken),
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private async Task StartReplyAsync(
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
                        await _pipeline.ProcessMessageAsync(
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

#endif
