using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using static HotChocolate.Transport.Sockets.Delimiter;

namespace HotChocolate.Transport.Sockets;

internal sealed class MessageProcessor
{
    private readonly IMessageHandler _messageHandler;
    private readonly PipeReader _reader;

    public MessageProcessor(IMessageHandler messageHandler, PipeReader reader)
    {
        _messageHandler = messageHandler;
        _reader = reader;
    }

    public async Task ProcessMessagesAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                SequencePosition? position;
                var result = await _reader.ReadAsync(cancellationToken)
                    .ConfigureAwait(false);
                var buffer = result.Buffer;

                do
                {
                    position = buffer.PositionOf(EndOfText);

                    if (position is not null)
                    {
                        await _messageHandler.OnReceiveAsync(
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
            await _reader.CompleteAsync().ConfigureAwait(false);
        }
    }
}
