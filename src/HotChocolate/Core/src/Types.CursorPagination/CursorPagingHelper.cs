using System;
using System.Collections;
using System.Collections.Generic;
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

            Range range = SliceRange<TEntity>(arguments, maxElementCount);

            var skip = range.Start;
            var take = range.Count();

            // we fetch one element more than we requested
            if (take != maxElementCount)
            {
                take++;
            }

            TSource slicedSource = source;
            if (skip != 0)
            {
                slicedSource = applySkip(source, skip);
            }

            if (take != maxElementCount)
            {
                slicedSource = applyTake(slicedSource, take);
            }

            IReadOnlyList<IndexEdge<TEntity>> selectedEdges =
                await toIndexEdgesAsync(slicedSource, skip, cancellationToken);

            bool moreItemsReturnedThanRequested = selectedEdges.Count > range.Count();
            bool isSequenceFromStart = range.Start == 0;

            selectedEdges = new SkipLastCollection<IndexEdge<TEntity>>(
                selectedEdges,
                moreItemsReturnedThanRequested);

            ConnectionPageInfo pageInfo =
                CreatePageInfo(isSequenceFromStart, moreItemsReturnedThanRequested, selectedEdges);

            return new Connection<TEntity>(
                selectedEdges,
                pageInfo,
                async ct => await countAsync(source, ct));
        }

        private static ConnectionPageInfo CreatePageInfo<TEntity>(
            bool isSequenceFromStart,
            bool moreItemsReturnedThanRequested,
            IReadOnlyList<IndexEdge<TEntity>> selectedEdges)
        {
            // We know that there is a next page if more items than requested are returned
            bool hasNextPage = moreItemsReturnedThanRequested;

            // There is a previous page if the sequence start is not 0.
            // If you point to index 2 of a empty list, we assume that there is a previous page
            bool hasPreviousPage = !isSequenceFromStart;

            IndexEdge<TEntity>? firstEdge = null;
            IndexEdge<TEntity>? lastEdge = null;

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

        private static Range SliceRange<TEntity>(
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

            Range range = new(startIndex, before);

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
            range.Take(first);

            //[SPEC] if last is less than 0 throw an error
            ValidateLast(arguments, out int? last);
            //[SPEC] Slice edges to be of length last by removing edges from the start of edges.
            range.TakeLast(last);

            return range;
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

        internal class Range
        {
            public Range(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; private set; }

            public int End { get; private set; }

            public int Count()
            {
                if (End < Start)
                {
                    return 0;
                }

                return End - Start;
            }


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
