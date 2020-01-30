using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Utilities.Subscriptions
{
    internal sealed class AsyncEnumerableStreamAdapter<T>
        : IAsyncEnumerable<object>
    {
        private readonly IAsyncEnumerable<T> _enumerable;

        public AsyncEnumerableStreamAdapter(IAsyncEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
        }

        public async IAsyncEnumerator<object> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            await foreach (T item in _enumerable.WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }
}
