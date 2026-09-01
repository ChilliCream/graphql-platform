using System.Threading.Channels;
#if FUSION
using HotChocolate.Buffers;
using HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;
#else
using HotChocolate.Utilities;
#endif

#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols;
#else
namespace HotChocolate.Transport.Sockets.Client.Protocols;
#endif

internal sealed class DataMessageObserver : IObserver<IOperationMessage>, IDisposable
{
    private readonly Channel<IDataMessage> _channel = Channel.CreateUnbounded<IDataMessage>();
    private readonly string _id;
#if FUSION
    private readonly IMemoryArenaSource _arenaSource;
    private readonly bool _deferPayloadParsing;
    private readonly int _maxQueueBytes;
    private readonly IDataCompletion _completion;
    private Exception? _terminalError;
    private int _queuedBytes;
#endif

#if FUSION
    public DataMessageObserver(
        string id,
        IMemoryArenaSource arenaSource,
        bool deferPayloadParsing,
        int maxQueueBytes,
        IDataCompletion completion)
    {
        _id = id;
        _arenaSource = arenaSource;
        _deferPayloadParsing = deferPayloadParsing;
        _maxQueueBytes = maxQueueBytes;
        _completion = completion;
    }
#else
    public DataMessageObserver(string id)
    {
        _id = id;
    }

    public bool TryHandle(IDataMessage message)
    {
        if (!message.Id.EqualsOrdinal(_id))
        {
            return false;
        }

        OnNext(message);
        return true;
    }
#endif

    public async ValueTask<IDataMessage?> TryReadNextAsync(CancellationToken ct)
    {
#if FUSION
        ThrowIfTerminated();
#endif

        // WaitToReadAsync rethrows the error the channel was completed with (for example a
        // SocketClosedException) so it surfaces to the consumer, and returns false on a
        // clean completion.
        while (await _channel.Reader.WaitToReadAsync(ct))
        {
#if FUSION
            ThrowIfTerminated();
#endif

            if (_channel.Reader.TryRead(out var message))
            {
#if FUSION
                if (message is FusionDataMessage dataMessage)
                {
                    Interlocked.Add(ref _queuedBytes, -dataMessage.PayloadLength);
                    ThrowIfTerminated(dataMessage);

                    if (_deferPayloadParsing)
                    {
                        try
                        {
                            dataMessage.ParsePayload(_arenaSource);
                        }
                        catch
                        {
                            dataMessage.Dispose();
                            throw;
                        }
                    }
                }
#endif
                return message;
            }
        }

        return null;
    }

    public void OnNext(IOperationMessage value)
    {
#if FUSION
        if (value is FusionDataMessage dataMessage && dataMessage.TryClaim(_id))
        {
            if (!TryReserve(dataMessage.PayloadLength))
            {
                dataMessage.Dispose();
                FailQueueOverflow();
                return;
            }

            try
            {
                if (!_deferPayloadParsing)
                {
                    dataMessage.ParsePayload(_arenaSource);
                }

                if (!_channel.Writer.TryWrite(dataMessage))
                {
                    Interlocked.Add(ref _queuedBytes, -dataMessage.PayloadLength);
                    dataMessage.Dispose();
                }
            }
            catch
            {
                Interlocked.Add(ref _queuedBytes, -dataMessage.PayloadLength);
                dataMessage.Dispose();
                throw;
            }
        }
        else if (value is IDataMessage message
            && string.Equals(message.Id, _id, StringComparison.Ordinal)
            && !_channel.Writer.TryWrite(message))
        {
            message.Dispose();
        }
#else
        if (value is IDataMessage message && message.Id.EqualsOrdinal(_id))
        {
            // the channel may already be completed (for example after the result was disposed),
            // in which case the message is dropped and must release its pooled buffers here.
            if (!_channel.Writer.TryWrite(message))
            {
                message.Dispose();
            }
        }
#endif
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
#if FUSION
            if (message is FusionDataMessage dataMessage)
            {
                Interlocked.Add(ref _queuedBytes, -dataMessage.PayloadLength);
            }
#endif
            message.Dispose();
        }
    }

#if FUSION
    private bool TryReserve(int bytes)
    {
        while (true)
        {
            if (Volatile.Read(ref _terminalError) is not null)
            {
                return false;
            }

            var queuedBytes = Volatile.Read(ref _queuedBytes);
            if (bytes > _maxQueueBytes - queuedBytes)
            {
                return false;
            }

            if (Interlocked.CompareExchange(
                    ref _queuedBytes,
                    queuedBytes + bytes,
                    queuedBytes)
                == queuedBytes)
            {
                return true;
            }
        }
    }

    private void FailQueueOverflow()
    {
        var error = ThrowHelper.OperationQueueCapacityExceeded(_id, _maxQueueBytes);

        if (Interlocked.CompareExchange(ref _terminalError, error, null) is null)
        {
            _completion.TrySendCompleteMessage();
            _channel.Writer.TryComplete(error);

            while (_channel.Reader.TryRead(out var queuedMessage))
            {
                if (queuedMessage is FusionDataMessage dataMessage)
                {
                    Interlocked.Add(ref _queuedBytes, -dataMessage.PayloadLength);
                }

                queuedMessage.Dispose();
            }
        }
    }

    private void ThrowIfTerminated(FusionDataMessage? message = null)
    {
        if (Volatile.Read(ref _terminalError) is { } error)
        {
            message?.Dispose();
            throw error;
        }
    }
#endif
}
