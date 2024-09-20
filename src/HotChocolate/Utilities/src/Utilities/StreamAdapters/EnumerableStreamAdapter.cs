using System.Collections;

namespace HotChocolate.Utilities.StreamAdapters;

internal sealed class EnumerableStreamAdapter : IAsyncEnumerable<object?>
{
    private readonly IEnumerable _enumerable;

    public EnumerableStreamAdapter(IEnumerable enumerable)
    {
        _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
    }

    public IAsyncEnumerator<object?> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
        => new Enumerator(_enumerable, cancellationToken);

    private sealed class Enumerator : IAsyncEnumerator<object?>, IDisposable
    {
        private readonly IEnumerator _enumerator;
        private readonly IDisposable? _disposable;
        private readonly CancellationToken _cancellationToken;
        private bool _disposed;

        public Enumerator(IEnumerable enumerable, CancellationToken cancellationToken)
        {
            _enumerator = enumerable.GetEnumerator();
            _disposable = _enumerator as IDisposable;
            _cancellationToken = cancellationToken;
        }

        public object? Current => _enumerator.Current;

        public ValueTask<bool> MoveNextAsync()
            => _cancellationToken.IsCancellationRequested
                ? new ValueTask<bool>(false)
                : new ValueTask<bool>(_enumerator.MoveNext());

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposable?.Dispose();
            _disposed = true;
        }
    }
}

internal sealed class EnumerableStreamAdapter<T> : IAsyncEnumerable<object?>
{
    private readonly IEnumerable<T> _enumerable;

    public EnumerableStreamAdapter(IEnumerable<T> enumerable)
    {
        _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
    }

    public IAsyncEnumerator<object?> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
        => new Enumerator(_enumerable, cancellationToken);

    private sealed class Enumerator : IAsyncEnumerator<object?>, IDisposable
    {
        private readonly IEnumerator _enumerator;
        private readonly IDisposable? _disposable;
        private readonly CancellationToken _cancellationToken;
        private bool _disposed;

        public Enumerator(IEnumerable<T> enumerator, CancellationToken cancellationToken)
        {
            _enumerator = enumerator.GetEnumerator();
            _disposable = _enumerator as IDisposable;
            _cancellationToken = cancellationToken;
        }

        public object? Current => _enumerator.Current;

        public ValueTask<bool> MoveNextAsync()
            => _cancellationToken.IsCancellationRequested
                ? new ValueTask<bool>(false)
                : new ValueTask<bool>(_enumerator.MoveNext());

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposable?.Dispose();
            _disposed = true;
        }
    }
}
