using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StrawberryShake.Http.Subscriptions
{
    /// <summary>
    /// Event handler to receive message data
    /// </summary>
    /// <param name="messageData">The sequence of bytes of this message</param>
    /// <param name="cancellationToken">The cancellation token of the action</param>
    internal delegate ValueTask ProcessAsync(
        ReadOnlySequence<byte> messageData,
        CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to <see cref="PipeReader"/> and executes <see cref="ProcessAsync"/> whenever
    /// a message was fully received
    /// </summary>
    internal static class MessageProcessor
    {
        /// <summary>
        /// The Delimiter that separated two messages
        /// </summary>
        internal const byte Delimiter = 0x07;

        /// <summary>
        /// Subscribes to <see cref="PipeReader"/> and executes <see cref="ProcessAsync"/> whenever
        /// a message was fully received
        /// </summary>
        /// <param name="reader">The reader that provides the data</param>
        /// <param name="processAsync">
        /// The event handler that is invoked every time a message is fully parsed
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token that cancels
        /// </param>
        /// <returns>
        /// A task that is completed when <paramref name="cancellationToken"/> is cancelled
        /// </returns>
        public static Task Start(
            PipeReader reader,
            ProcessAsync processAsync,
            CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(
                () => ProcessMessagesAsync(reader, processAsync, cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private static async Task ProcessMessagesAsync(
            PipeReader reader,
            ProcessAsync processAsync,
            CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    ReadResult result = await reader
                        .ReadAsync(cancellationToken)
                        .ConfigureAwait(false);

                    ReadOnlySequence<byte> buffer = result.Buffer;
                    SequencePosition? position = null;

                    do
                    {
                        position = buffer.PositionOf(Delimiter);

                        if (position != null)
                        {
                            await processAsync(
                                    buffer.Slice(0, position.Value),
                                    cancellationToken)
                                .ConfigureAwait(false);

                            // Skip the message which was read.
                            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                        }
                    } while (position != null);

                    reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                await reader.CompleteAsync().ConfigureAwait(false);
            }
        }
    }
}
