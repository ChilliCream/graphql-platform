using System.Linq.Expressions;
using GreenDonut.Data.Cursors;
using GreenDonut.Data.Expressions;
using static GreenDonut.Data.Expressions.ExpressionHelpers;

namespace GreenDonut.Data.Internal;

/// <summary>
/// Composes the shared slicing logic (validation, cursor parsing, predicate building,
/// order inversion and skip) that the <see cref="PagingQueryableExtensions"/> paging APIs
/// apply before executing a query.
/// </summary>
internal static class PagingQueryComposer
{
    /// <summary>
    /// Validates the paging arguments against <paramref name="source"/> and slices the
    /// query up to, but not including, the final <c>Take</c> that limits the page size.
    /// </summary>
    /// <param name="source">
    /// The queryable to be paged.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <param name="includeTotalCount">
    /// If set to <c>true</c> the total count will be included in the result.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns the composed query, ready to be limited by the caller and executed.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified, if both <c>first</c> and
    /// <c>last</c> are specified, if <c>first</c> or <c>last</c> is given and not greater than
    /// zero, if <c>before</c> is combined with a relative <c>after</c> cursor, if an end cursor
    /// is used without <c>before</c> and <c>last</c>, if an end cursor in <c>before</c> is
    /// combined with <c>after</c>, or if the end cursor's skip or a relative cursor's offset
    /// does not fit into an <see cref="int"/>.
    /// </exception>
    public static PagingQueryComposition<T> Compose<T>(
        IQueryable<T> source,
        PagingArguments arguments,
        bool includeTotalCount)
    {
        ArgumentNullException.ThrowIfNull(source);

        source = QueryHelpers.EnsureOrderPropsAreSelected(source);
        Expression<Func<T, T>>? selector = null;
        var applySelectorAfterPaging = arguments.After is not null || arguments.Before is not null;

        if (applySelectorAfterPaging)
        {
            selector = QueryHelpers.ExtractCurrentSelector(source);

            if (selector is not null)
            {
                source = QueryHelpers.RemoveSelector(source);
            }
        }

        var keys = ParseDataSetKeys(source);

        if (keys.Length == 0)
        {
            throw new ArgumentException(
                "In order to use cursor pagination, you must specify at least one key using the `OrderBy` method.",
                nameof(source));
        }

        if (arguments.Last is not null && arguments.First is not null)
        {
            throw new ArgumentException(
                "You can specify either `first` or `last`, but not both as this can lead to unpredictable results.",
                nameof(arguments));
        }

        ValidateRequestedCount(arguments);

        if (arguments.First is null && arguments.Last is null)
        {
            arguments = arguments with { First = 10 };
        }

        // Relative cursors with no incoming cursor require an initial count of the dataset.
        if (arguments.EnableRelativeCursors
            && string.IsNullOrEmpty(arguments.After)
            && string.IsNullOrEmpty(arguments.Before))
        {
            includeTotalCount = true;
        }

        var originalQuery = source;
        var direction = arguments.Last is null
            ? PagingDirection.Forward
            : PagingDirection.Backward;
        var requestedCount = direction is PagingDirection.Forward
            ? arguments.First!.Value
            : arguments.Last!.Value;
        var offset = 0;
        int? totalCount = null;
        var usesRelativeCursors = false;
        Cursor? cursor = null;

        // The exact skip count for an end-cursor page, or null otherwise.
        int? endCursorSkip = null;

        if (arguments.After is not null)
        {
            cursor = CursorParser.Parse(arguments.After, keys);

            if (cursor.IsEndCursor)
            {
                throw ThrowHelper.PagingArguments_EndCursorRequiresBeforeAndLast();
            }

            var (whereExpr, cursorOffset) = BuildWhereExpression<T>(
                keys,
                cursor,
                true,
                arguments.NullOrdering);
            source = source.Where(whereExpr);
            offset = cursorOffset;

            if (!includeTotalCount)
            {
                totalCount ??= cursor.TotalCount;
            }

            if (cursor.IsRelative)
            {
                usesRelativeCursors = true;
            }
        }

        if (arguments.Before is not null)
        {
            if (usesRelativeCursors)
            {
                throw new ArgumentException(
                    "You cannot use `before` and `after` with relative cursors at the same time.",
                    nameof(arguments));
            }

            cursor = CursorParser.Parse(arguments.Before, keys);

            if (cursor.IsEndCursor)
            {
                if (arguments.First is not null || arguments.Last is null)
                {
                    throw ThrowHelper.PagingArguments_EndCursorRequiresBeforeAndLast();
                }

                if (arguments.After is not null)
                {
                    throw ThrowHelper.PagingArguments_EndCursorBeforeCombinedWithAfter();
                }

                offset = cursor.Offset!.Value;

                // The count is always inlined for an end-cursor page; the cached total is only
                // used to position the skip for pages before the last one.
                includeTotalCount = true;

                endCursorSkip = GetEndCursorSkip(cursor, requestedCount);
            }
            else
            {
                var (whereExpr, cursorOffset) = BuildWhereExpression<T>(
                    keys,
                    cursor,
                    false,
                    arguments.NullOrdering);
                source = source.Where(whereExpr);
                offset = cursorOffset;

                if (!includeTotalCount)
                {
                    totalCount ??= cursor.TotalCount;
                }
            }
        }

        if (cursor?.IsRelative == true)
        {
            if ((arguments.Last is not null && cursor.Offset > 0)
                || (arguments.First is not null && cursor.Offset < 0))
            {
                throw new ArgumentException(
                    "Positive offsets are not allowed with `last`, and negative offsets are not allowed with `first`.",
                    nameof(arguments));
            }
        }

        if (direction is PagingDirection.Backward)
        {
            source = ReverseOrderExpressionRewriter.Rewrite(source);
        }

        if (endCursorSkip is not null)
        {
            if (endCursorSkip.Value > 0)
            {
                source = source.Skip(endCursorSkip.Value);
            }
        }
        else
        {
            var absOffset = Math.Abs((long)offset);

            if (absOffset > 0)
            {
                source = source.Skip(CheckedOffsetSkip(absOffset, requestedCount));
            }
        }

        return new PagingQueryComposition<T>(
            originalQuery,
            source,
            keys,
            cursor,
            direction,
            requestedCount,
            offset,
            selector,
            arguments,
            includeTotalCount,
            totalCount);
    }

    /// <summary>
    /// Extracts the cursor keys from the order expressions of <paramref name="source"/>.
    /// </summary>
    /// <param name="source">
    /// The queryable to extract the keys from.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns the cursor keys extracted from the queryable's order expressions.
    /// </returns>
    public static CursorKey[] ParseDataSetKeys<T>(IQueryable<T> source)
    {
        var parser = new CursorKeyParser();
        parser.Visit(source.Expression);
        return [.. parser.Keys];
    }

    /// <summary>
    /// Validates that <c>first</c> and <c>last</c> are greater than zero when given.
    /// </summary>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <exception cref="ArgumentException">
    /// If <c>first</c> or <c>last</c> is given and not greater than zero.
    /// </exception>
    internal static void ValidateRequestedCount(PagingArguments arguments)
    {
        if (arguments.First is <= 0)
        {
            throw ThrowHelper.PagingArguments_FirstMustBeGreaterThanZero();
        }

        if (arguments.Last is <= 0)
        {
            throw ThrowHelper.PagingArguments_LastMustBeGreaterThanZero();
        }
    }

    /// <summary>
    /// Multiplies a relative cursor's absolute offset by the requested page size in checked
    /// long arithmetic, to slice from the start or end of the dataset by whole pages.
    /// </summary>
    /// <param name="absOffset">
    /// The absolute value of the cursor's relative offset.
    /// </param>
    /// <param name="requestedCount">
    /// The number of items requested through <c>first</c> or <c>last</c>.
    /// </param>
    /// <returns>
    /// Returns the number of items to skip.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the product does not fit into an <see cref="int"/>.
    /// </exception>
    internal static int CheckedOffsetSkip(long absOffset, int requestedCount)
    {
        var skip = absOffset * requestedCount;

        if (skip > int.MaxValue)
        {
            throw ThrowHelper.PagingArguments_RelativeOffsetOutOfRange();
        }

        return (int)skip;
    }

    /// <summary>
    /// Computes how many items to skip from the end of the dataset for an end-cursor page, from
    /// the cursor's cached total and the requested page size.
    /// </summary>
    /// <param name="cursor">
    /// The parsed end cursor.
    /// </param>
    /// <param name="requestedCount">
    /// The number of items requested through <c>last</c>.
    /// </param>
    /// <returns>
    /// Returns the number of items to skip, or zero for the last page.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the computed skip does not fit into an <see cref="int"/>.
    /// </exception>
    internal static int GetEndCursorSkip(Cursor cursor, int requestedCount)
    {
        var pagesBeforeLast = -(long)cursor.Offset!.Value;

        if (pagesBeforeLast == 0)
        {
            return 0;
        }

        var cachedTotal = cursor.TotalCount!.Value;
        var remainder = cachedTotal % requestedCount == 0
            ? requestedCount
            : cachedTotal % requestedCount;
        var skip = remainder + (pagesBeforeLast - 1) * requestedCount;

        if (skip > int.MaxValue)
        {
            throw ThrowHelper.PagingArguments_EndCursorOffsetOutOfRange();
        }

        return (int)skip;
    }

    /// <summary>
    /// Computes how many items to skip from the end of each key's own group for a batch
    /// end-cursor page that is not the last one, positioning a window twice the requested page
    /// size wide enough to contain the exact page.
    /// </summary>
    /// <param name="pagesBeforeLast">
    /// The number of pages before the last one, or zero for the last page.
    /// </param>
    /// <param name="requestedCount">
    /// The number of items requested through <c>last</c>.
    /// </param>
    /// <returns>
    /// Returns the number of items to skip, or zero for the last page.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the computed skip, or twice the requested count, does not fit into an
    /// <see cref="int"/>.
    /// </exception>
    internal static int GetBatchEndCursorSkip(int pagesBeforeLast, int requestedCount)
    {
        if (pagesBeforeLast <= 0)
        {
            return 0;
        }

        if ((long)requestedCount * 2 > int.MaxValue)
        {
            throw ThrowHelper.PagingArguments_BatchWindowTooLargeForPageSize();
        }

        var skip = (long)(pagesBeforeLast - 1) * requestedCount;

        if (skip > int.MaxValue)
        {
            throw ThrowHelper.PagingArguments_EndCursorOffsetOutOfRange();
        }

        return (int)skip;
    }
}
