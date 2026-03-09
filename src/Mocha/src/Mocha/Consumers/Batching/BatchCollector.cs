namespace Mocha;

/// <summary>
/// Buffers incoming messages and emits them as batches based on size or time thresholds.
/// </summary>
internal sealed class BatchCollector<TEvent> : IAsyncDisposable
{
    private readonly Func<MessageBatch<TEvent>, ValueTask> _onBatchReady;
    private readonly int _maxBatchSize;

    private readonly object _sync = new();
    private readonly DelayedAction _delay;
    private List<BufferedEntry<TEvent>> _buffer = [];
    private bool _disposed;

    public BatchCollector(
        BatchOptions options,
        Func<MessageBatch<TEvent>, ValueTask> onBatchReady,
        TimeProvider timeProvider)
    {
        _onBatchReady = onBatchReady;
        _maxBatchSize = options.MaxBatchSize;
        _delay = new DelayedAction(options.BatchTimeout, timeProvider, OnDelayElapsed);
    }

    /// <summary>
    /// Adds a context to the buffer. If the buffer reaches <see cref="_maxBatchSize"/>,
    /// the batch is emitted via the callback with back-pressure.
    /// </summary>
    public async ValueTask<BufferedEntry<TEvent>> Add(IConsumeContext<TEvent> context)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var entry = new BufferedEntry<TEvent>(context);
        MessageBatch<TEvent>? batch = null;

        lock (_sync)
        {
            _buffer.Add(entry);

            if (_buffer.Count >= _maxBatchSize)
            {
                _delay.Cancel();
                batch = FlushBufferLocked(BatchCompletionMode.Size);
            }
            else if (_buffer.Count == 1)
            {
                _delay.Start();
            }
        }

        if (batch is not null)
        {
            await _onBatchReady(batch);
        }

        return entry;
    }

    private async ValueTask OnDelayElapsed()
    {
        MessageBatch<TEvent>? batch;

        lock (_sync)
        {
            if (_disposed || _buffer.Count == 0)
            {
                return;
            }

            batch = FlushBufferLocked(BatchCompletionMode.Time);
        }

        await _onBatchReady(batch);
    }

    private MessageBatch<TEvent> FlushBufferLocked(BatchCompletionMode mode)
    {
        var batch = new MessageBatch<TEvent>(_buffer, mode);
        _buffer = [];
        return batch;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        MessageBatch<TEvent>? remaining = null;

        lock (_sync)
        {
            _disposed = true;
            _delay.Cancel();

            if (_buffer.Count > 0)
            {
                remaining = new MessageBatch<TEvent>(_buffer, BatchCompletionMode.Forced);
                _buffer = [];
            }
        }

        await _delay.DisposeAsync();

        if (remaining is not null)
        {
            await _onBatchReady(remaining);
        }
    }
}
