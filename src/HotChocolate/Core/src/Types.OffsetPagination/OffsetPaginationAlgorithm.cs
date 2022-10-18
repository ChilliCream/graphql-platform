using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// This base class is a helper class for offset paging handlers and contains the basic
/// algorithm for offset pagination.
/// </summary>
/// <typeparam name="TQuery">
/// The type representing the query builder.
/// </typeparam>
/// <typeparam name="TEntity">
/// The entity type.
/// </typeparam>
public abstract class OffsetPaginationAlgorithm<TQuery, TEntity>
{
    /// <summary>
    /// Applies the pagination algorithm to the provided data.
    /// </summary>
    /// <param name="query">The query builder.</param>
    /// <param name="arguments">The paging arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="totalCountInSelection"></param>
    /// <param name="itemsInSelection"></param>
    /// <returns></returns>
    public ValueTask<CollectionSegment<TEntity>> ApplyPaginationAsync(TQuery query,
        OffsetPagingArguments arguments,
        CancellationToken cancellationToken, bool totalCountInSelection, bool itemsInSelection) =>
        ApplyPaginationAsync(query, arguments, null, totalCountInSelection, itemsInSelection, cancellationToken);

    /// <summary>
    /// Applies the pagination algorithm to the provided data.
    /// </summary>
    /// <param name="query">The query builder.</param>
    /// <param name="arguments">The paging arguments.</param>
    /// <param name="totalCount">Specify the total amount of elements</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="totalCountInSelection">Specify whether the field 'totalCount' is presented in the selection set</param>
    /// <param name="itemsInSelection">Specify whether the field 'items' is presented in the selection set</param>
    public async ValueTask<CollectionSegment<TEntity>> ApplyPaginationAsync(
        TQuery query,
        OffsetPagingArguments arguments,
        int? totalCount,
        bool totalCountInSelection,
        bool itemsInSelection,
        CancellationToken cancellationToken)
    {
        if (!totalCountInSelection && !itemsInSelection)
            throw new InvalidOperationException($"{nameof(totalCountInSelection)} and {nameof(itemsInSelection)} cannot be both false");

        Func<CancellationToken, ValueTask<int>> getTotalCount = totalCount is null
            ? async ct => await CountAsync(query, ct)
            : _ => new ValueTask<int>(totalCount.Value);

        var hasPreviousPage = (arguments.Skip ?? 0) > 0;

        bool hasNextPage;
        Func<CancellationToken, ValueTask<IReadOnlyCollection<TEntity>>> getItems;

        TQuery sliced = query;

        if (itemsInSelection)
        {
            if (arguments.Skip is { } skip)
            {
                sliced = ApplySkip(sliced, skip);
            }

            if (arguments.Take is { } take)
            {
                sliced = ApplyTake(sliced, take + 1);
            }

            IReadOnlyList<TEntity> items =
                await ExecuteAsync(sliced, cancellationToken).ConfigureAwait(false);

            hasNextPage = items.Count == arguments.Take + 1;

            items = new SkipLastCollection<TEntity>(items, skipLast: hasNextPage);

            getItems = _ => new ValueTask<IReadOnlyCollection<TEntity>>(items);
        }
        else
        {
            hasNextPage = arguments.Take != null &&
                          totalCount.Value - arguments.Skip.GetValueOrDefault(0) > arguments.Take;

            getItems = _ => throw new InvalidOperationException("The 'items' field is not in the selection set");
        }

        CollectionSegmentInfo pageInfo = new(hasNextPage, hasPreviousPage);

        return new CollectionSegment<TEntity>(getItems, pageInfo, getTotalCount);
    }

    /// <summary>
    /// Override this method to apply a skip on top of the provided query.
    /// </summary>
    protected abstract TQuery ApplySkip(TQuery query, int skip);

    /// <summary>
    /// Override this method to apply a take (limit) on top of the provided query.
    /// </summary>
    protected abstract TQuery ApplyTake(TQuery query, int take);

    /// <summary>
    /// Override this to implement a count function on top of the provided query.
    /// </summary>
    protected abstract ValueTask<int> CountAsync(
        TQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Override this to implement the query execution.
    /// </summary>
    protected abstract ValueTask<IReadOnlyList<TEntity>> ExecuteAsync(
        TQuery query,
        CancellationToken cancellationToken);

    private static ValueTask<int> GetTotalCountAssert(CancellationToken _) =>
        throw new InvalidOperationException();

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
}
