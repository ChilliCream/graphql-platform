using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination;

/// <summary>
/// This base class is a helper class for cursor paging handlers and contains the basic
/// algorithm for cursor pagination.
/// </summary>
/// <typeparam name="TQuery">
/// The type representing the query builder.
/// </typeparam>
/// <typeparam name="TEntity">
/// The entity type.
/// </typeparam>
public abstract class CursorPaginationAlgorithm<TQuery, TEntity> where TQuery : notnull
{
    /// <summary>
    /// Applies the pagination algorithm to the provided query.
    /// </summary>
    /// <param name="query">The query builder.</param>
    /// <param name="arguments">The paging arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns the connection.
    /// </returns>
    public ValueTask<Connection<TEntity>> ApplyPaginationAsync(
        TQuery query,
        CursorPagingArguments arguments,
        CancellationToken cancellationToken) =>
        ApplyPaginationAsync(query, arguments, null, cancellationToken);

    /// <summary>
    /// Applies the pagination algorithm to the provided query.
    /// </summary>
    /// <param name="query">The query builder.</param>
    /// <param name="arguments">The paging arguments.</param>
    /// <param name="totalCount">Specify the total amount of elements</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns the connection.
    /// </returns>
    public async ValueTask<Connection<TEntity>> ApplyPaginationAsync(
        TQuery query,
        CursorPagingArguments arguments,
        int? totalCount,
        CancellationToken cancellationToken)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var maxElementCount = int.MaxValue;
        Func<CancellationToken, ValueTask<int>> executeCount = totalCount is null
            ? ct => CountAsync(query, ct)
            : _ => new ValueTask<int>(totalCount.Value);

        // We only need the maximal element count if no `before` counter is set and no `first`
        // argument is provided.
        if (arguments.Before is null && arguments.First is null)
        {
            var count = await executeCount(cancellationToken);
            maxElementCount = count;

            // in case we already know the total count, we set the totalCount parameter
            // so that we do not have have to fetch the count twice
            executeCount = _ => new(count);
        }

        CursorPagingRange range = SliceRange(arguments, maxElementCount);

        var skip = range.Start;
        var take = range.Count();

        // we fetch one element more than we requested
        if (take != maxElementCount)
        {
            take++;
        }

        TQuery slicedSource = query;
        if (skip != 0)
        {
            slicedSource = ApplySkip(query, skip);
        }

        if (take != maxElementCount)
        {
            slicedSource = ApplyTake(slicedSource, take);
        }

        IReadOnlyList<Edge<TEntity>> selectedEdges =
            await ExecuteAsync(slicedSource, skip, cancellationToken);

        var moreItemsReturnedThanRequested = selectedEdges.Count > range.Count();
        var isSequenceFromStart = range.Start == 0;

        selectedEdges = new SkipLastCollection<Edge<TEntity>>(
            selectedEdges,
            moreItemsReturnedThanRequested);

        ConnectionPageInfo pageInfo =
            CreatePageInfo(isSequenceFromStart, moreItemsReturnedThanRequested, selectedEdges);

        return new Connection<TEntity>(selectedEdges, pageInfo, executeCount);
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
    protected abstract ValueTask<IReadOnlyList<Edge<TEntity>>> ExecuteAsync(
        TQuery query,
        int offset,
        CancellationToken cancellationToken);

    private static ConnectionPageInfo CreatePageInfo(
        bool isSequenceFromStart,
        bool moreItemsReturnedThanRequested,
        IReadOnlyList<Edge<TEntity>> selectedEdges)
    {
        // We know that there is a next page if more items than requested are returned
        var hasNextPage = moreItemsReturnedThanRequested;

        // There is a previous page if the sequence start is not 0.
        // If you point to index 2 of a empty list, we assume that there is a previous page
        var hasPreviousPage = !isSequenceFromStart;

        Edge<TEntity>? firstEdge = null;
        Edge<TEntity>? lastEdge = null;

        if (selectedEdges.Count > 0)
        {
            firstEdge = selectedEdges[0];
            lastEdge = selectedEdges[selectedEdges.Count - 1];
        }

        return new ConnectionPageInfo(
            hasNextPage,
            hasPreviousPage,
            firstEdge?.Cursor,
            lastEdge?.Cursor);
    }

    private static CursorPagingRange SliceRange(
        CursorPagingArguments arguments,
        int maxElementCount)
    {
        // [SPEC] if after is set then remove all elements of edges before and including
        // afterEdge.
        //
        // The cursor is increased by one so that the index points to the element after
        var startIndex = arguments.After is { } a
            ? IndexEdge<TEntity>.DeserializeCursor(a) + 1
            : 0;

        // [SPEC] if before is set then remove all elements of edges before and including
        // beforeEdge.
        var before = arguments.Before is { } b
            ? IndexEdge<TEntity>.DeserializeCursor(b)
            : maxElementCount;

        // if after is negative we have know how much of the offset was in the negative range.
        // The amount of positions that are in the negative range, have to be subtracted from
        // the take or we will fetch too many items.
        var startOffsetCorrection = 0;
        if (startIndex < 0)
        {
            startOffsetCorrection = Math.Abs(startIndex);
            startIndex = 0;
        }

        CursorPagingRange range = new(startIndex, before);

        //[SPEC] If first is less than 0 throw an error
        ValidateFirst(arguments, out var first);

        if (first is not null)
        {
            first -= startOffsetCorrection;
            if (first < 0)
            {
                first = 0;
            }
        }

        //[SPEC] Slice edges to be of length first by removing edges from the end of edges.
        range.Take(first);

        //[SPEC] if last is less than 0 throw an error
        ValidateLast(arguments, out var last);

        //[SPEC] Slice edges to be of length last by removing edges from the start of edges.
        range.TakeLast(last);

        return range;
    }

    private sealed class SkipLastCollection<T> : IReadOnlyList<T>
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

    private static void ValidateFirst(
        CursorPagingArguments arguments,
        out int? first)
    {
        if (arguments.First < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(first));
        }

        first = arguments.First;
    }

    private static void ValidateLast(
        CursorPagingArguments arguments,
        out int? last)
    {
        if (arguments.Last < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(last));
        }

        last = arguments.Last;
    }
}
