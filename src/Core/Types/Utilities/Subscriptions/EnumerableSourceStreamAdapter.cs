using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

        public IAsyncEnumerator<object> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            return new Enumerator(_enumerable.GetEnumerator(), cancellationToken);
        }

        private sealed class Enumerator
            : IAsyncEnumerator<object>
        {
            private readonly IEnumerator<T> _enumerator;
            private readonly CancellationToken _cancellationToken;

            public Enumerator(IEnumerator<T> enumerator, CancellationToken cancellationToken)
            {
                _enumerator = enumerator;
                _cancellationToken = cancellationToken;
            }

            public object Current { get; private set; }

            public ValueTask<bool> MoveNextAsync()
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    Current = null;
                    return new ValueTask<bool>(false);
                }

                bool result = _enumerator.MoveNext();
                Current = result ? (object)_enumerator.Current : null;
                return new ValueTask<bool>(result);
            }

            public ValueTask DisposeAsync() => default;
        }
    }
}
