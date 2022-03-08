using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using HotChocolate.Utilities.Transport.Sockets.Helpers;

namespace HotChocolate.Utilities.Transport.Sockets.Protocols;

internal abstract class MessageProcessorBase
{
    private readonly PipeReader _reader;

    protected MessageProcessorBase(PipeReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public void Begin(CancellationToken cancellationToken)
        => Task.Factory.StartNew(
            () => ProcessMessagesAsync(cancellationToken),
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskScheduler.Default);

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
                    position = buffer.PositionOf(Constants.Delimiter);

                    if (position is not null)
                    {
                        await ProcessMessageAsync(
                            buffer.Slice(0, position.Value),
                            cancellationToken)
                            .ConfigureAwait(false);

                        // Skip the message which was read.
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                } while (position != null);

                _reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // we will just stop receiving
        }
        catch (WebSocketException)
        {
            // we will just stop receiving
        }
        finally
        {
            // reader should be completed always, so that related pipe writer can
            // stop write new messages
            await _reader.CompleteAsync();
        }
    }

    protected abstract ValueTask ProcessMessageAsync(
        ReadOnlySequence<byte> message,
        CancellationToken cancellationToken);

    protected abstract ValueTask CompletedAsync();
}
