using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace HotChocolate.Types.Selections
{
    public static class AsyncCursorSourceExtensions
    {
        public static IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this IAsyncCursorSource<T> asyncCursorSource) =>
                new AsyncEnumerableAdapter<T>(asyncCursorSource);

        private class AsyncEnumerableAdapter<T> : IAsyncEnumerable<T>
        {
            private readonly IAsyncCursorSource<T> _asyncCursorSource;

            public AsyncEnumerableAdapter(IAsyncCursorSource<T> asyncCursorSource)
            {
                _asyncCursorSource = asyncCursorSource;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(
                CancellationToken cancellationToken = default) =>
                    new AsyncEnumeratorAdapter<T>(_asyncCursorSource, cancellationToken);
        }

        private class AsyncEnumeratorAdapter<T> : IAsyncEnumerator<T>
        {
            private readonly IAsyncCursorSource<T> _asyncCursorSource;
            private readonly CancellationToken _cancellationToken;
            private IAsyncCursor<T> _asyncCursor;
            private IEnumerator<T> _batchEnumerator;

            public T Current => _batchEnumerator.Current;

            public AsyncEnumeratorAdapter(
                IAsyncCursorSource<T> asyncCursorSource,
                CancellationToken cancellationToken)
            {
                _asyncCursorSource = asyncCursorSource;
                _cancellationToken = cancellationToken;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_asyncCursor is null)
                {
                    _asyncCursor = await _asyncCursorSource
                        .ToCursorAsync(_cancellationToken)
                        .ConfigureAwait(false);
                }

                if (_batchEnumerator?.MoveNext() == true)
                {
                    return true;
                }

                if (await _asyncCursor?.MoveNextAsync(_cancellationToken))
                {
                    _batchEnumerator?.Dispose();
                    _batchEnumerator = _asyncCursor.Current.GetEnumerator();
                    return _batchEnumerator.MoveNext();
                }

                return false;
            }

            public ValueTask DisposeAsync()
            {
                _asyncCursor?.Dispose();
                _asyncCursor = null;
                return default;
            }
        }
    }
}
