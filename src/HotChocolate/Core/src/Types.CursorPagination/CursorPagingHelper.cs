using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types.Pagination
{
    public static class CursorPagingHelper
    {
        public delegate ValueTask<IReadOnlyList<IndexEdge<TEntity>>>
            ToIndexEdgesAsync<TSource, TEntity>(
            TSource source,
            int offset,
            CancellationToken cancellationToken);

        public delegate TSource ApplySkip<TSource>(TSource source, int skip);

        public delegate TSource ApplyTake<TSource>(TSource source, int take);

        public delegate ValueTask<int> CountAsync<TSource>(
            TSource source,
            CancellationToken cancellationToken);

        public static async ValueTask<Connection> ApplyPagination<TSource, TEntity>(
            TSource source,
            CursorPagingArguments arguments,
            ApplySkip<TSource> applySkip,
            ApplyTake<TSource> applyTake,
            ToIndexEdgesAsync<TSource, TEntity> toIndexEdgesAsync,
            CountAsync<TSource> countAsync,
            CancellationToken cancellationToken = default)
        {
            // We only need the maximal element count if no `before` counter is set and no `first`
            // argument is provided.
            var maxElementCount = int.MaxValue;
            if (arguments.Before is null && arguments.First is null)
            {
                var count = await countAsync(source, cancellationToken);
                maxElementCount = count;

                // in case we already know the total count, we override the countAsync parameter
                // so that we do not have to fetch the count twice
                countAsync = (_, _) => new ValueTask<int>(count);
            }

            // [SPEC] if after is set then remove all elements of edges before and including
            // afterEdge.
            //
            // The cursor is increased by one so that the index points to the element after
            var startIndex = arguments.After is { } a
                ? IndexEdge<TEntity>.DeserializeCursor(a) + 1
                : 0;

            // [SPEC] if before is set then remove all elements of edges before and including
            // beforeEdge.
            int before = arguments.Before is { } b
                ? IndexEdge<TEntity>.DeserializeCursor(b)
                : maxElementCount;

            // if after is negative we have know how much of the offset was in the negative range.
            // The amount of positions that are in the negative range, have to be subtracted from
            // the take or we will fetch too many items.
            int startOffsetCorrection = 0;
            if (startIndex < 0)
            {
                startOffsetCorrection = Math.Abs(startIndex);
                startIndex = 0;
            }

            Range edges = new(startIndex, before);

            //[SPEC] If first is less than 0 throw an error
            ValidateFirst(arguments, out int? first);
            if (first is { })
            {
                first -= startOffsetCorrection;
                if (first < 0)
                {
                    first = 0;
                }
            }

            //[SPEC] Slice edges to be of length first by removing edges from the end of edges.
            edges.Take(first);

            //[SPEC] if last is less than 0 throw an error
            ValidateLast(arguments, out int? last);
            //[SPEC] Slice edges to be of length last by removing edges from the start of edges.
            edges.TakeLast(last);


            // In case last was not provided we assume that we do forward pagination. We do need
            // to know the direction of the pagination to know where the overfetched element is
            var isForwardPagination = arguments.Last is null;
            var isSequenceFromStart = edges.Start == 0;
            bool skipIsReducedByOne = false;

            var skip = edges.Start;
            var take = edges.Count();

            if (isForwardPagination)
            {
                // in case we do forward pagination we want to have one element more than requested
                // at the end of the sequence. This element shows us if there is a next page
                if (take != maxElementCount)
                {
                    take++;
                }
            }
            else
            {
                if (!isSequenceFromStart)
                {
                    // in case we do backwards pagination we want to have an additional element at
                    // the start of the sequence. There for we reduce the skip by one and increase
                    // take by one.
                    skipIsReducedByOne = true;
                    skip--;
                    if (take != int.MaxValue)
                    {
                        take++;
                    }
                }
            }

            TSource slicedSource = applySkip(source, skip);
            if (take != maxElementCount)
            {
                slicedSource = applyTake(slicedSource, take);
            }

            IReadOnlyList<IndexEdge<TEntity>> selectedEdges =
                await toIndexEdgesAsync(slicedSource, skip, cancellationToken);

            bool lessItemsReturnedThanRequested = selectedEdges.Count < edges.Count();
            bool moreItemsReturnedThanRequested = selectedEdges.Count > edges.Count();

            // There can only be a next page if the last edge does not point to the last element in
            // the list. We either know this because of the maxElementCount or because we
            // overfetched
            bool hasNextPage = edges.End < maxElementCount;
            bool hasPreviousPage = edges.Start > 0;
            IndexEdge<TEntity>? firstEdge = null;
            IndexEdge<TEntity>? lastEdge = null;

            // we know that we over-fetched if more items are returned than requested.
            // we always over-fetched if skip was reduced by one
            bool isOverfetched = moreItemsReturnedThanRequested || skipIsReducedByOne;

            if (isForwardPagination)
            {
                if (isOverfetched)
                {
                    // in case that we do forward pagination and we over-fetched, the additional
                    // element is at the end of the list.
                    // We know that there is a next page
                    selectedEdges =
                        new SkipFirstOrLastCollection<IndexEdge<TEntity>>(
                            selectedEdges,
                            skipLast: true);

                    selectedEdges.Take(edges.Count()).ToArray();
                    hasNextPage = true;
                }
                else
                {
                    // if no additional item is returned, we know that there is no next page
                    hasNextPage = false;
                }

                // we do not need to check for `lessItemsReturnedThanRequested` as we corrected
                // the start index already
            }
            else
            {
                if (isOverfetched)
                {
                    // in case that we do backward pagination and we over-fetched, the
                    // additional element is at the beginning of the list.
                    // we skip this element and know that there is a previous page
                    selectedEdges =
                        new SkipFirstOrLastCollection<IndexEdge<TEntity>>(
                            selectedEdges,
                            skipFirst: true);

                    hasPreviousPage = true;
                }
                else
                {
                    // in case no additional element is returned, there is no previous page
                    hasPreviousPage = false;
                }

                if (lessItemsReturnedThanRequested)
                {
                    // if less item than requested are returned, we also know that there is
                    // no next page
                    hasNextPage = false;
                }
            }

            if (selectedEdges.Count > 0)
            {
                firstEdge = selectedEdges[0];
                lastEdge = selectedEdges[selectedEdges.Count - 1];
            }

            var pageInfo = new ConnectionPageInfo(
                hasNextPage,
                hasPreviousPage,
                firstEdge?.Cursor,
                lastEdge?.Cursor,
                0);

            return new Connection<TEntity>(
                selectedEdges,
                pageInfo,
                async ct => await countAsync(source, ct));
        }

        private class SkipFirstOrLastCollection<T> : IReadOnlyList<T>
        {
            private readonly IReadOnlyList<T> _items;
            private readonly bool _skipFirst;
            private readonly bool _skipLast;

            public SkipFirstOrLastCollection(
                IReadOnlyList<T> items,
                bool skipFirst = false,
                bool skipLast = false)
            {
                _items = items;
                _skipFirst = skipFirst;
                _skipLast = skipLast;
                Count = _items.Count;

                if (skipFirst)
                {
                    Count--;
                }

                if (skipLast)
                {
                    Count--;
                }
            }

            public int Count { get; }

            public IEnumerator<T> GetEnumerator()
            {
                for (var i = 0; i < _items.Count; i++)
                {
                    if (i == 0 && _skipFirst)
                    {
                        continue;
                    }

                    if (i == _items.Count - 1 && _skipLast)
                    {
                        break;
                    }

                    yield return _items[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public T this[int index] => _skipFirst ? _items[index + 1] : _items[index];
        }

        public class Range
        {
            public Range(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; private set; }

            public int End { get; private set; }

            public int Count() => End - Start;

            public void Take(int? first)
            {
                if (first is { })
                {
                    var end = Start + first.Value;
                    if (End > end)
                    {
                        End = end;
                    }
                }
            }

            public void TakeLast(int? last)
            {
                if (last is { })
                {
                    var start = End - last.Value;
                    if (Start < start)
                    {
                        Start = start;
                    }
                }
            }
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
}
