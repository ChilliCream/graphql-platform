using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using HotChocolate.AspNetCore.Subscriptions.Protocols;

namespace HotChocolate.AspNetCore.Subscriptions;

internal sealed class MessageProcessor
{
    private readonly ISocketConnection _connection;
    private readonly PipeReader _reader;

    public MessageProcessor(ISocketConnection connection, PipeReader reader)
    {
        _connection = connection;
        _reader = reader;
    }

    public void Begin(
        IProtocolHandler protocolHandler,
        CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(
            () => ProcessMessagesAsync(protocolHandler, cancellationToken),
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private async Task ProcessMessagesAsync(
        IProtocolHandler protocolHandler,
        CancellationToken cancellationToken)
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
                        await protocolHandler.ExecuteAsync(
                            _connection,
                            buffer.Slice(0, position.Value),
                            cancellationToken);

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
}
