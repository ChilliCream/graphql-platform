using System.Collections.Generic;
using System.Threading;

namespace HotChocolate.Utilities.Subscriptions
{
    internal sealed class EnumerableSourceStreamAdapter<T>
        : IAsyncEnumerable<object>
    {
        private readonly IEnumerable<T> _enumerable;

        public EnumerableSourceStreamAdapter(IEnumerable<T> enumerable)
        {
            _enumerable = enumerable;
        }

#pragma warning disable CS1998
        public async IAsyncEnumerator<object> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            foreach (T item in _enumerable)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                yield return item;
            }
        }
#pragma warning restore CS1998
    }
}
