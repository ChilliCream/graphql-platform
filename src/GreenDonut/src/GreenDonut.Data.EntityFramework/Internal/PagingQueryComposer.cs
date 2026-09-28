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
    /// <c>last</c> are specified, if <c>before</c> is combined with a relative
    /// <c>after</c> cursor, or if an end cursor is used without <c>before</c> and
    /// <c>last</c>.
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

        if (arguments.First is null && arguments.Last is null)
        {
            arguments = arguments with { First = 10 };
        }

        // if relative cursors are enabled and no cursor is provided
        // we must do an initial count of the dataset.
        if (arguments.EnableRelativeCursors
            && string.IsNullOrEmpty(arguments.After)
            && string.IsNullOrEmpty(arguments.Before))
        {
            includeTotalCount = true;
        }

        var originalQuery = source;
        var forward = arguments.Last is null;
        var requestedCount = forward ? arguments.First!.Value : arguments.Last!.Value;
        var offset = 0;
        int? totalCount = null;
        var usesRelativeCursors = false;
        Cursor? cursor = null;

        // the exact skip count for an end cursor page, or null if the cursor is not an end
        // cursor. It replaces the generic offset-based skip below because it is derived from
        // the dataset total rather than from a fixed number of pages.
        int? endCursorSkip = null;

        if (arguments.After is not null)
        {
            cursor = CursorParser.Parse(arguments.After, keys);

            if (cursor.IsEndCursor)
            {
                throw new ArgumentException(
                    "An end cursor is only valid when used with `before` and `last`.",
                    nameof(arguments));
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
                    throw new ArgumentException(
                        "An end cursor is only valid when used with `before` and `last`.",
                        nameof(arguments));
                }

                offset = cursor.Offset!.Value;
                var cachedTotal = cursor.TotalCount!.Value;
                var pagesBeforeLast = -offset;

                // the count is always inlined for an end cursor page, as the cached total on
                // the cursor may be stale. The cached total is only used to position the skip
                // for pages before the last one.
                includeTotalCount = true;

                if (pagesBeforeLast == 0)
                {
                    endCursorSkip = 0;
                }
                else
                {
                    var remainder = cachedTotal % requestedCount == 0
                        ? requestedCount
                        : cachedTotal % requestedCount;
                    endCursorSkip = remainder + (pagesBeforeLast - 1) * requestedCount;
                }
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

        var isBackward = arguments.Last is not null;

        if (isBackward)
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
            var absOffset = Math.Abs(offset);

            if (absOffset > 0)
            {
                source = source.Skip(absOffset * requestedCount);
            }
        }

        return new PagingQueryComposition<T>(
            originalQuery,
            source,
            keys,
            cursor,
            forward,
            isBackward,
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
}

/// <summary>
/// The result of composing the shared paging slicing logic for a queryable.
/// </summary>
/// <param name="OriginalQuery">
/// The query before any cursor predicate, order inversion or skip was applied. Used to
/// compute the total count of the unsliced dataset.
/// </param>
/// <param name="SlicedQuery">
/// The query with the cursor predicate, order inversion and skip applied, but without the
/// final <c>Take</c> that limits the page size.
/// </param>
/// <param name="Keys">
/// The cursor keys extracted from the queryable's order expressions.
/// </param>
/// <param name="Cursor">
/// The parsed cursor, or <c>null</c> if neither <c>after</c> nor <c>before</c> was specified.
/// </param>
/// <param name="Forward">
/// <c>true</c> if the page is sliced from the start of the dataset (using <c>first</c>).
/// </param>
/// <param name="IsBackward">
/// <c>true</c> if the page is sliced from the end of the dataset (using <c>last</c>).
/// </param>
/// <param name="RequestedCount">
/// The number of items requested through <c>first</c> or <c>last</c>.
/// </param>
/// <param name="Offset">
/// The signed relative offset extracted from the cursor, or zero if none was specified.
/// </param>
/// <param name="Selector">
/// The selector extracted from the queryable, to be re-applied after paging, or
/// <c>null</c> if no selector needs to be re-applied.
/// </param>
/// <param name="Arguments">
/// The paging arguments, with defaults applied.
/// </param>
/// <param name="IncludeTotalCount">
/// If set to <c>true</c> the total count must be fetched as part of the page execution.
/// </param>
/// <param name="TotalCount">
/// The total count carried over from a relative cursor, or <c>null</c> if none is known yet.
/// </param>
/// <typeparam name="T">
/// The type of the items in the queryable.
/// </typeparam>
internal sealed record PagingQueryComposition<T>(
    IQueryable<T> OriginalQuery,
    IQueryable<T> SlicedQuery,
    CursorKey[] Keys,
    Cursor? Cursor,
    bool Forward,
    bool IsBackward,
    int RequestedCount,
    int Offset,
    Expression<Func<T, T>>? Selector,
    PagingArguments Arguments,
    bool IncludeTotalCount,
    int? TotalCount);
