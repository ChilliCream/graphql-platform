using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Utilities.StreamAdapters
{
    internal sealed class QueryableStreamAdapter : IAsyncEnumerable<object?>
    {
        private readonly IQueryable _query;

        public QueryableStreamAdapter(IQueryable query)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public async IAsyncEnumerator<object?> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            List<object?> list = await ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (var item in list)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                yield return item;
            }
        }

        private Task<List<object?>> ToListAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var items = new List<object?>();

                foreach (var o in _query)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    items.Add(o);
                }

                return items;
            }, cancellationToken);
        }
    }

    internal sealed class QueryableStreamAdapter<T> : IAsyncEnumerable<object?>
    {
        private readonly IQueryable<T> _query;

        public QueryableStreamAdapter(IQueryable<T> query)
        {
            _query = query ?? throw new ArgumentNullException(nameof(query));
        }

        public async IAsyncEnumerator<object?> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
        {
            List<T> list = await ToListAsync(cancellationToken).ConfigureAwait(false);

            foreach (T? item in list)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                yield return item;
            }
        }

        private Task<List<T>> ToListAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var items = new List<T>();

                foreach (T o in _query)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    items.Add(o);
                }

                return items;
            }, cancellationToken);
        }
    }
}
