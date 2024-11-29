using HotChocolate.Utilities;

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
    /// <param name="totalCount"></param>
    /// <returns>
    /// Returns the connection.
    /// </returns>
    public CursorPaginationAlgorithmResult<TQuery> ApplyPagination(
        TQuery query,
        CursorPagingArguments arguments,
        int? totalCount)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        totalCount ??= int.MaxValue;
        var range = SliceRange(arguments, totalCount.Value);

        var skip = range.Start;
        var take = range.Count();
        var overfetch = false;

        // we fetch one element more than we requested
        if (take != totalCount.Value)
        {
            take++;
            overfetch = true;
        }

        var slicedSource = query;

        if (skip != 0)
        {
            slicedSource = ApplySkip(slicedSource, skip);
        }

        if (take != totalCount.Value)
        {
            slicedSource = ApplyTake(slicedSource, take);
        }

        return new(slicedSource, skip, overfetch ? --take : take);
    }

    /// <summary>
    /// Override this method to apply a skip on top of the provided query.
    /// </summary>
    protected abstract TQuery ApplySkip(TQuery query, int skip);

    /// <summary>
    /// Override this method to apply a take (limit) on top of the provided query.
    /// </summary>
    protected abstract TQuery ApplyTake(TQuery query, int take);


    private static CursorPagingRange SliceRange(
        CursorPagingArguments arguments,
        int totalCount)
    {
        // [SPEC] if after is set then remove all elements of edges before and including
        // afterEdge.
        //
        // The cursor is increased by one so that the index points to the element after
        var startIndex = 0;
        if (arguments.After is not null)
        {
            if (!IndexCursor.TryParse(arguments.After, out var index))
            {
                throw ThrowHelper.InvalidIndexCursor("after", arguments.After);
            }

            startIndex = index + 1;
        }

        // [SPEC] if before is set then remove all elements of edges before and including
        // beforeEdge.
        var before = totalCount;
        if (arguments.Before is not null)
        {
            if (!IndexCursor.TryParse(arguments.Before, out var index))
            {
                throw ThrowHelper.InvalidIndexCursor("before", arguments.Before);
            }

            before = index;
        }

        // if after is negative we have know how much of the offset was in the negative range.
        // The amount of positions that are in the negative range, have to be subtracted from
        // the take, or we will fetch too many items.
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
