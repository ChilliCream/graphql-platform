using System.Threading.Channels;
#if !FUSION
using HotChocolate.Utilities;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols;
#else
namespace HotChocolate.Transport.Sockets.Client.Protocols;
#endif

internal sealed class DataMessageObserver(string id) : IObserver<IOperationMessage>, IDisposable
{
    private readonly Channel<IDataMessage> _channel = Channel.CreateUnbounded<IDataMessage>();

    public async ValueTask<IDataMessage?> TryReadNextAsync(CancellationToken ct)
    {
        // WaitToReadAsync rethrows the error the channel was completed with (for example a
        // SocketClosedException) so it surfaces to the consumer, and returns false on a
        // clean completion.
        while (await _channel.Reader.WaitToReadAsync(ct))
        {
            if (_channel.Reader.TryRead(out var message))
            {
                return message;
            }
        }

        return null;
    }

    public void OnNext(IOperationMessage value)
    {
#if FUSION
        if (value is IDataMessage message && string.Equals(message.Id, id, StringComparison.Ordinal))
#else
        if (value is IDataMessage message && message.Id.EqualsOrdinal(id))
#endif
        {
            // the channel may already be completed (for example after the result was disposed),
            // in which case the message is dropped and must release its pooled buffers here.
            if (!_channel.Writer.TryWrite(message))
            {
                message.Dispose();
            }
        }
    }

    public void OnError(Exception error)
        => _channel.Writer.TryComplete(error);

    public void OnCompleted()
        => _channel.Writer.TryComplete();

    public void Dispose()
    {
        _channel.Writer.TryComplete();

        // drain any messages that were written but never read so their pooled buffers are
        // returned instead of being stranded when the result is disposed.
        while (_channel.Reader.TryRead(out var message))
        {
            message.Dispose();
        }
    }
}
