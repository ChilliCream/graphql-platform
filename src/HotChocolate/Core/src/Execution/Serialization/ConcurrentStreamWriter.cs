using System.Collections.Concurrent;
using System.Threading.Channels;
using HotChocolate.Buffers;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Serialization;

internal sealed class ConcurrentStreamWriter : IAsyncDisposable
{
    private readonly Channel<PooledArrayWriter> _backlog;
    private readonly ConcurrentStack<PooledArrayWriter> _pool = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Stream _stream;
    private PooledArrayWriter? _firstBuffer;
    private bool _disposed;

    public ConcurrentStreamWriter(Stream stream, int backlogSize)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _stream = stream;
        _backlog = Channel.CreateBounded<PooledArrayWriter>(
            new BoundedChannelOptions(backlogSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

        ProcessBacklogAsync(_cts.Token).FireAndForget();
    }

    public void OnCompleted(Action<Exception?> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        // We discard the task as the continuation will be executed synchronously,
        // which means its execution will be inlined.
        _ = _backlog.Reader.Completion.ContinueWith(
            static (t, state) =>
            {
                if (t.IsFaulted)
                {
                    ((Action<Exception?>)state!).Invoke(t.Exception);
                }
                else
                {
                    ((Action<Exception?>)state!).Invoke(null);
                }
            },
            callback,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    public PooledArrayWriter Begin()
    {
        var buffer =  Interlocked.Exchange(ref _firstBuffer, null);

        if( buffer is not null)
        {
            return buffer;
        }

        if (_pool.TryPop(out buffer))
        {
            return buffer;
        }

        return new PooledArrayWriter();
    }

    public ValueTask CommitAsync(PooledArrayWriter bufferWriter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bufferWriter);

        return CommitInternalAsync(bufferWriter, cancellationToken);
    }

    public Task WaitForCompletionAsync()
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _backlog.Writer.TryComplete();
        return _backlog.Reader.Completion;
    }

    private async ValueTask CommitInternalAsync(PooledArrayWriter bufferWriter, CancellationToken cancellationToken)
        => await _backlog.Writer.WriteAsync(bufferWriter, cancellationToken).ConfigureAwait(false);

    private async Task ProcessBacklogAsync(CancellationToken ct)
    {
        const int maxCapacity = 1024 * 64;
        PooledArrayWriter? currentBuffer = null;

        try
        {
            await foreach (var message in _backlog.Reader.ReadAllAsync(ct))
            {
                try
                {
                    currentBuffer = message;
                    await _stream.WriteAsync(message.GetWrittenMemory(), ct).ConfigureAwait(false);
                    await _stream.FlushAsync(ct).ConfigureAwait(false);
                }
                finally
                {
                    if (message.Capacity > maxCapacity)
                    {
                        currentBuffer = null;
                        message.Dispose();
                    }
                    else
                    {
                        message.Reset();

                        if (Interlocked.CompareExchange(ref _firstBuffer, message, null) is null)
                        {
                            _pool.Push(message);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _backlog.Writer.TryComplete(ex);
        }

        currentBuffer?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _backlog.Writer.TryComplete(new InvalidOperationException(
            "The concurrent pipe writer has been disposed and can no longer be used."));

        await _cts.CancelAsync().ConfigureAwait(false);
        _cts.Dispose();

        // drain leftovers and return pooled memory.
        while (_backlog.Reader.TryRead(out var w))
        {
            w.Dispose();
        }

        while (_pool.TryPop(out var buffer))
        {
            buffer.Dispose();
        }

        if (_firstBuffer is not null)
        {
            _firstBuffer.Dispose();
            _firstBuffer = null;
        }
    }
}
