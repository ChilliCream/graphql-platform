using System.Collections.Immutable;
using System.Linq.Expressions;
using HotChocolate.Pagination.Expressions;
using static HotChocolate.Pagination.Expressions.ExpressionHelpers;

namespace HotChocolate.Pagination;

/// <summary>
/// Provides extension methods to page a queryable.
/// </summary>
public static class PagingQueryableExtensions
{
    private static readonly AsyncLocal<InterceptorHolder> _interceptor = new();

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
        => await source.ToPageAsync(arguments, includeTotalCount: false, cancellationToken);

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
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

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

        var originalQuery = source;
        var forward = arguments.Last is null;
        var requestedCount = int.MaxValue;

        if (arguments.After is not null)
        {
            var cursor = CursorParser.Parse(arguments.After, keys);
            source = source.Where(BuildWhereExpression<T>(keys, cursor, true));
        }

        if (arguments.Before is not null)
        {
            var cursor = CursorParser.Parse(arguments.Before, keys);
            source = source.Where(BuildWhereExpression<T>(keys, cursor, false));
        }

        if (arguments.First is not null)
        {
            source = source.Take(arguments.First.Value + 1);
            requestedCount = arguments.First.Value;
        }

        if (arguments.Last is not null)
        {
            source = source.Reverse().Take(arguments.Last.Value + 1);
            requestedCount = arguments.Last.Value;
        }

        var builder = ImmutableArray.CreateBuilder<T>();
        int? totalCount = null;
        var fetchCount = 0;

        if (includeTotalCount)
        {
            var combinedQuery = source.Select(t => new { TotalCount = originalQuery.Count(), Item = t });

            TryGetQueryInterceptor()?.OnBeforeExecute(combinedQuery);

            await foreach (var item in combinedQuery.AsAsyncEnumerable()
                .WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                totalCount ??= item.TotalCount;
                fetchCount++;

                if (fetchCount > requestedCount)
                {
                    break;
                }

                builder.Add(item.Item);
            }
        }
        else
        {
            TryGetQueryInterceptor()?.OnBeforeExecute(source);

            await foreach (var item in source.AsAsyncEnumerable()
                .WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                fetchCount++;

                if (fetchCount > requestedCount)
                {
                    break;
                }

                builder.Add(item);
            }
        }

        if (builder.Count == 0)
        {
            return Page<T>.Empty;
        }

        if (!forward)
        {
            builder.Reverse();
        }

        return CreatePage(builder.ToImmutable(), arguments, keys, fetchCount, totalCount);
    }

    /// <summary>
    /// Executes a batch query with paging and returns the selected pages for each parent.
    /// </summary>
    /// <param name="source">
    /// The queryable to be paged.
    /// </param>
    /// <param name="keySelector">
    /// A function to select the key of the parent.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of the parent key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified.
    /// </exception>
    public static ValueTask<Dictionary<TKey, Page<TValue>>> ToBatchPageAsync<TKey, TValue>(
        this IQueryable<TValue> source,
        Expression<Func<TValue, TKey>> keySelector,
        PagingArguments arguments,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => ToBatchPageAsync<TKey, TValue, TValue>(source, keySelector, t => t, arguments, cancellationToken);

    /// <summary>
    /// Executes a batch query with paging and returns the selected pages for each parent.
    /// </summary>
    /// <param name="source">
    /// The queryable to be paged.
    /// </param>
    /// <param name="keySelector">
    /// A function to select the key of the parent.
    /// </param>
    /// <param name="valueSelector">
    /// A function to select the value of the items in the queryable.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of the parent key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <typeparam name="TElement">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified.
    /// </exception>
    public static async ValueTask<Dictionary<TKey, Page<TValue>>> ToBatchPageAsync<TKey, TValue, TElement>(
        this IQueryable<TElement> source,
        Expression<Func<TElement, TKey>> keySelector,
        Func<TElement, TValue> valueSelector,
        PagingArguments arguments,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
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

        source = QueryHelpers.EnsureOrderPropsAreSelected(source);

        // we need to move the ordering into the select expression we are constructing
        // so that the groupBy will not remove it. The first thing we do here is to extract the order expressions
        // and to create a new expression that will not contain it anymore.
        var ordering = ExtractAndRemoveOrder(source.Expression);

        var forward = arguments.Last is null;
        var requestedCount = int.MaxValue;
        var selectExpression =
            BuildBatchSelectExpression<TKey, TElement>(
                arguments,
                keys,
                ordering.OrderExpressions,
                ordering.OrderMethods,
                forward,
                ref requestedCount);
        var map = new Dictionary<TKey, Page<TValue>>();

        // we apply our new expression here.
        source = source.Provider.CreateQuery<TElement>(ordering.Expression);

        TryGetQueryInterceptor()?.OnBeforeExecute(source.GroupBy(keySelector).Select(selectExpression));

        await foreach (var item in source
            .GroupBy(keySelector)
            .Select(selectExpression)
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            if (item.Items.Count == 0)
            {
                map.Add(item.Key, Page<TValue>.Empty);
                continue;
            }

            var itemCount = requestedCount > item.Items.Count ? item.Items.Count : requestedCount;
            var builder = ImmutableArray.CreateBuilder<TValue>(itemCount);

            for (var i = 0; i < itemCount; i++)
            {
                builder.Add(valueSelector(item.Items[i]));
            }

            var page = CreatePage(builder.ToImmutable(), arguments, keys, item.Items.Count);
            map.Add(item.Key, page);
        }

        return map;
    }

    private static Page<T> CreatePage<T>(
        ImmutableArray<T> items,
        PagingArguments arguments,
        CursorKey[] keys,
        int fetchCount,
        int? totalCount = null)
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

        return new Page<T>(
            items,
            hasNext,
            hasPrevious,
            item => CursorFormatter.Format(item, keys),
            totalCount);
    }

    private static CursorKey[] ParseDataSetKeys<T>(IQueryable<T> source)
    {
        var parser = new CursorKeyParser();
        parser.Visit(source.Expression);
        return parser.Keys.ToArray();
    }

    private sealed class InterceptorHolder
    {
        public PagingQueryInterceptor? Interceptor { get; set; }
    }

    private static PagingQueryInterceptor? TryGetQueryInterceptor()
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
