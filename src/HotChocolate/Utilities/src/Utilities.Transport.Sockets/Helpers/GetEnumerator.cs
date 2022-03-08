using System.Collections.Concurrent;

namespace HotChocolate.Utilities.Transport.Sockets.Helpers;

internal sealed class GetEnumerator<TSource> : IAsyncEnumerator<TSource>, IObserver<TSource>
{
    private readonly ConcurrentQueue<TSource> _queue;
    private TSource? _current;
    private Exception? _error;
    private bool _done;
    private bool _disposed;
    private IDisposable? _subscription;
    private readonly SemaphoreSlim _gate;
    private readonly CancellationToken _cancellationToken;

    public GetEnumerator(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _queue = new ConcurrentQueue<TSource>();
        _gate = new SemaphoreSlim(0);
    }

    public IAsyncEnumerator<TSource> Run(IObservable<TSource> source)
    {
        _subscription = source.Subscribe(this);
        return this;
    }

    public void OnNext(TSource value)
    {
        _queue.Enqueue(value);
        _gate.Release();
    }

    public void OnError(Exception error)
    {
        _error = error;
        _subscription?.Dispose();
        _gate.Release();
    }

    public void OnCompleted()
    {
        _done = true;
        _subscription?.Dispose();
        _gate.Release();
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        if (_done)
        {
            return false;
        }

        await _gate.WaitAsync(_cancellationToken);

        if (_disposed)
        {
            throw new ObjectDisposedException("The enumerator was already disposed.");
        }

        if (_queue.TryDequeue(out _current))
        {
            return true;
        }

        if (_error is not null)
        {
            throw _error;
        }

        _gate.Release();
        return false;
    }

    public TSource Current => _current!;

    public void Reset() => throw new NotSupportedException();

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _subscription?.Dispose();
            _gate.Release();
            _gate.Dispose();
            _disposed = true;
        }

        return default;
    }
}
