using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        => new Enumerator(_enumerable.GetEnumerator(), cancellationToken);

    private sealed class Enumerator : IAsyncEnumerator<object?>
    {
        private readonly IEnumerator _enumerator;
        private readonly CancellationToken _cancellationToken;

        public Enumerator(IEnumerator enumerator, CancellationToken cancellationToken)
        {
            _enumerator = enumerator;
            _cancellationToken = cancellationToken;
        }

        public object? Current => _enumerator.Current;

        public ValueTask<bool> MoveNextAsync()
            => _cancellationToken.IsCancellationRequested
                ? new ValueTask<bool>(false)
                : new ValueTask<bool>(_enumerator.MoveNext());

        public ValueTask DisposeAsync()
        {
            if (_enumerator is IDisposable d)
            {
                d.Dispose();
            }
            return default;
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
        => new Enumerator(_enumerable.GetEnumerator(), cancellationToken);

    private sealed class Enumerator : IAsyncEnumerator<object?>
    {
        private readonly IEnumerator<T> _enumerator;
        private readonly CancellationToken _cancellationToken;

        public Enumerator(IEnumerator<T> enumerator, CancellationToken cancellationToken)
        {
            _enumerator = enumerator;
            _cancellationToken = cancellationToken;
        }

        public object? Current => _enumerator.Current;

        public ValueTask<bool> MoveNextAsync()
            => _cancellationToken.IsCancellationRequested
                ? new ValueTask<bool>(false)
                : new ValueTask<bool>(_enumerator.MoveNext());

        public ValueTask DisposeAsync()
        {
            _enumerator.Dispose();
            return default;
        }
    }
}
