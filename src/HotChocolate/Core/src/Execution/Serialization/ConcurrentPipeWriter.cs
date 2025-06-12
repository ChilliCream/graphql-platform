using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading.Channels;
using HotChocolate.Buffers;
using HotChocolate.Utilities;

namespace HotChocolate.Execution.Serialization;

internal sealed class ConcurrentPipeWriter : IDisposable
{
    private readonly Channel<PooledArrayWriter> _backlog;
    private readonly ConcurrentStack<PooledArrayWriter> _pool = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly PipeWriter _writer;
    private bool _disposed;

    public ConcurrentPipeWriter(PipeWriter writer, int backlogSize)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
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

        // We discard the task as the continuation will be executed synchronously
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
        if (_pool.TryPop(out var buffer))
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
        if(_disposed)
        {
            return Task.CompletedTask;
        }

        return _backlog.Reader.Completion;
    }

    private async ValueTask CommitInternalAsync(PooledArrayWriter bufferWriter, CancellationToken cancellationToken)
        => await _backlog.Writer.WriteAsync(bufferWriter, cancellationToken).ConfigureAwait(false);

    private async Task ProcessBacklogAsync(CancellationToken ct)
    {
        const int maxCapacity = 1024 * 64;
        Exception? exception = null;
        PooledArrayWriter? currentBuffer = null;

        try
        {
            await foreach (var message in _backlog.Reader.ReadAllAsync(ct))
            {
                try
                {
                    currentBuffer = message;
                    _writer.Write(message.GetWrittenSpan());
                    await _writer.FlushAsync(ct).ConfigureAwait(false);
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
                        _pool.Push(message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _backlog.Writer.TryComplete(ex);
            exception = ex;
        }

        currentBuffer?.Dispose();
        await _writer.CompleteAsync(exception).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _cts.Cancel();
        _backlog.Writer.Complete();
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
    }
}
