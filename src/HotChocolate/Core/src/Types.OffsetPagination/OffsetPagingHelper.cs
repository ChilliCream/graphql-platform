using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    public static class OffsetPagingHelper
    {
        public delegate ValueTask<IReadOnlyList<TEntity>>
            Execute<TSource, TEntity>(
            TSource source,
            CancellationToken cancellationToken);

        public delegate TSource ApplySkip<TSource>(TSource source, int skip);

        public delegate TSource ApplyTake<TSource>(TSource source, int take);

        public delegate ValueTask<int> CountAsync<TSource>(
            TSource source,
            CancellationToken cancellationToken);

        public static async ValueTask<CollectionSegment> ApplyPagination<TSource, TEntity>(
            TSource source,
            OffsetPagingArguments arguments,
            ApplySkip<TSource> applySkip,
            ApplyTake<TSource> applyTake,
            Execute<TSource, TEntity> execute,
            CountAsync<TSource> countAsync,
            CancellationToken cancellationToken = default)
        {
            TSource sliced = source;

            if (arguments.Skip is {} skip)
            {
                sliced = applySkip(sliced, skip);
            }

            if (arguments.Take is { } take)
            {
                sliced = applyTake(sliced, take + 1);
            }

            IReadOnlyList<TEntity> items =
                await execute(sliced, cancellationToken).ConfigureAwait(false);

            bool hasNextPage = items.Count == arguments.Take + 1;
            bool hasPreviousPage = (arguments.Skip ?? 0) > 0;

            CollectionSegmentInfo pageInfo = new(hasNextPage, hasPreviousPage);

            items = new SkipLastCollection<TEntity>(items, skipLast: hasNextPage);

            return new CollectionSegment(
                (IReadOnlyCollection<object>)items,
                pageInfo,
                async ct => await countAsync(source, ct));
        }

        private class SkipLastCollection<T> : IReadOnlyList<T>
        {
            private readonly IReadOnlyList<T> _items;
            private readonly bool _skipLast;

            public SkipLastCollection(
                IReadOnlyList<T> items,
                bool skipLast = false)
            {
                _items = items;
                _skipLast = skipLast;
                Count = _items.Count;

                if (skipLast && Count > 0)
                {
                    Count--;
                }
            }

            public int Count { get; }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Count; i++)
                {
                    if (i == _items.Count - 1 && _skipLast)
                    {
                        break;
                    }

                    yield return _items[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public T this[int index] => _items[index];
        }

        internal static async ValueTask<IReadOnlyList<TItemType>> ExecuteEnumerable<TItemType>(
            IEnumerable<TItemType> queryable,
            CancellationToken cancellationToken)
        {
            var list = new List<TItemType>();

            if (queryable is IAsyncEnumerable<TItemType> enumerable)
            {
                await foreach (TItemType item in enumerable.WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    list.Add(item);
                }
            }
            else
            {
                await Task.Run(() =>
                {
                    foreach (TItemType item in queryable)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        list.Add(item);
                    }

                }).ConfigureAwait(false);
            }

            return list;
        }
    }
}
