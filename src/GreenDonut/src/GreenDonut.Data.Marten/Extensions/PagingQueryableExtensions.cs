using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using GreenDonut.Data.Cursors;
using GreenDonut.Data.Expressions;
using Marten;
using static GreenDonut.Data.Expressions.ExpressionHelpers;


// ReSharper disable once CheckNamespace
namespace GreenDonut.Data;

/// <summary>
/// Provides extension methods to page a queryable.
/// </summary>
public static class PagingQueryableExtensions
{
    private static readonly AsyncLocal<InterceptorHolder> _interceptor = new();
    private static readonly ConcurrentDictionary<(Type, Type), Expression> _countExpressionCache = new();

    /// <summary>
    /// Executes a query with paging and returns the selected page.
    /// </summary>
    /// <param name="source">
    /// The queryable to be paged.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns a page of items.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified.
    /// </exception>
    public static async ValueTask<Page<T>> ToPageAsync<T>(
        this IQueryable<T> source,
        PagingArguments arguments,
        CancellationToken cancellationToken = default)
        => await source.ToPageAsync(arguments, includeTotalCount: arguments.IncludeTotalCount, cancellationToken);

    /// <summary>
    /// Executes a query with paging and returns the selected page.
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
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns a page of items.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified.
    /// </exception>
    public static async ValueTask<Page<T>> ToPageAsync<T>(
        this IQueryable<T> source,
        PagingArguments arguments,
        bool includeTotalCount,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        source = QueryHelpers.EnsureOrderPropsAreSelected(source);

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

        if (arguments.After is not null)
        {
            cursor = CursorParser.Parse(arguments.After, keys);
            var (whereExpr, cursorOffset) = BuildWhereExpression<T>(keys, cursor, true);
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
            var (whereExpr, cursorOffset) = BuildWhereExpression<T>(keys, cursor, false);
            source = source.Where(whereExpr);
            offset = cursorOffset;

            if (!includeTotalCount)
            {
                totalCount ??= cursor.TotalCount;
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

        var absOffset = Math.Abs(offset);

        if (absOffset > 0)
        {
            source = source.Skip(absOffset * requestedCount);
        }

        source = source.Take(requestedCount + 1);

        var builder = ImmutableArray.CreateBuilder<T>();
        var fetchCount = 0;

        if (includeTotalCount)
        {
            totalCount ??= await originalQuery
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            TryGetQueryInterceptor()?.OnBeforeExecute(source);

            await foreach (var item in source.ToAsyncEnumerable(cancellationToken)
                .ConfigureAwait(false))
            {
                fetchCount++;

                builder.Add(item);

                if (fetchCount > requestedCount)
                {
                    break;
                }
            }
        }
        else
        {
            TryGetQueryInterceptor()?.OnBeforeExecute(source);

            await foreach (var item in source.ToAsyncEnumerable(cancellationToken)
                .ConfigureAwait(false))
            {
                fetchCount++;

                builder.Add(item);

                if (fetchCount > requestedCount)
                {
                    break;
                }
            }
        }

        if (builder.Count == 0)
        {
            return Page<T>.Empty;
        }

        if (isBackward)
        {
            builder.Reverse();
        }

        if (builder.Count > requestedCount)
        {
            builder.RemoveAt(isBackward ? 0 : requestedCount);
        }

        var pageIndex = CreateIndex(arguments, cursor, totalCount);
        return CreatePage(builder.ToImmutable(), arguments, keys, fetchCount, pageIndex, requestedCount, totalCount);
    }

    private static Page<T> CreatePage<T>(
        ImmutableArray<T> items,
        PagingArguments arguments,
        CursorKey[] keys,
        int fetchCount,
        int? index,
        int? requestedPageSize,
        int? totalCount)
    {
        var hasPrevious = false;
        var hasNext = false;

        // if we skipped over an item, and we have fetched some items
        // than we have a previous page as we skipped over at least
        // one item.
        if (arguments.After is not null && fetchCount > 0)
        {
            hasPrevious = true;
        }

        // if we required the last 5 items of a dataset and over-fetch by 1
        // than we have a previous page.
        if (arguments.Last is not null && fetchCount > arguments.Last)
        {
            hasPrevious = true;
        }

        // if we request the first 5 items of a dataset with or without cursor
        // and we over-fetched by 1 item we have a next page.
        if (arguments.First is not null && fetchCount > arguments.First)
        {
            hasNext = true;
        }

        // if we fetched anything before an item we know that here is at least one more item.
        if (arguments.Before is not null)
        {
            hasNext = true;
        }

        if (arguments.EnableRelativeCursors && totalCount is not null && requestedPageSize is not null)
        {
            return new Page<T>(
                items,
                hasNext,
                hasPrevious,
                (item, o, p, c) => CursorFormatter.Format(item, keys, new CursorPageInfo(o, p, c)),
                index ?? 1,
                requestedPageSize.Value,
                totalCount.Value);
        }

        return new Page<T>(
            items,
            hasNext,
            hasPrevious,
            item => CursorFormatter.Format(item, keys),
            totalCount);
    }

    private static int? CreateIndex(PagingArguments arguments, Cursor? cursor, int? totalCount)
    {
        if (totalCount is not null
            && arguments.Last is not null
            && arguments.After is null
            && arguments.Before is null)
        {
            return Math.Max(1, (int)Math.Ceiling(totalCount.Value / (double)arguments.Last.Value));
        }

        if (cursor?.IsRelative != true)
        {
            return null;
        }

        if (arguments.After is not null)
        {
            if (arguments.First is not null)
            {
                return (cursor.PageIndex ?? 1) + (cursor.Offset ?? 0) + 1;
            }

            if (arguments.Last is not null && totalCount is not null)
            {
                return Math.Max(1, (int)Math.Ceiling(totalCount.Value / (double)arguments.Last.Value));
            }
        }

        if (arguments.Before is not null)
        {
            if (arguments.First is not null)
            {
                return 1;
            }

            if (arguments.Last is not null)
            {
                return (cursor.PageIndex ?? 1) - Math.Abs(cursor.Offset ?? 0) - 1;
            }
        }

        return null;
    }

    private static CursorKey[] ParseDataSetKeys<T>(IQueryable<T> source)
    {
        var parser = new CursorKeyParser();
        parser.Visit(source.Expression);
        return [.. parser.Keys];
    }

    private sealed class InterceptorHolder
    {
        public PagingQueryInterceptor? Interceptor { get; set; }
    }

    internal static PagingQueryInterceptor? TryGetQueryInterceptor()
        => _interceptor.Value?.Interceptor;

    internal static void SetQueryInterceptor(PagingQueryInterceptor pagingQueryInterceptor)
    {
        if (_interceptor.Value is null)
        {
            _interceptor.Value = new InterceptorHolder();
        }

        _interceptor.Value.Interceptor = pagingQueryInterceptor;
    }

    internal static void ClearQueryInterceptor(PagingQueryInterceptor pagingQueryInterceptor)
    {
        if (_interceptor.Value is not null)
        {
            _interceptor.Value.Interceptor = null;
        }
    }
}
