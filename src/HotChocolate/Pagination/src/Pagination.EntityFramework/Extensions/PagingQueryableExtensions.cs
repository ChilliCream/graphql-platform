using System.Collections.Immutable;
using System.Linq.Expressions;
using HotChocolate.Pagination.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using static HotChocolate.Pagination.Expressions.ExpressionHelpers;

namespace HotChocolate.Pagination;

/// <summary>
/// Provides extension methods to page a queryable.
/// </summary>
public static class PagingQueryableExtensions
{
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

        var keys = ParseDataSetKeys(source);

        if (keys.Length == 0)
        {
            throw new ArgumentException(
                "In order to use cursor pagination, you must specify at least on key using the `OrderBy` method.",
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
            source = source.Where(BuildWhereExpression<T>(keys, cursor, forward));
        }

        if (arguments.Before is not null)
        {
            var cursor = CursorParser.Parse(arguments.Before, keys);
            source = source.Where(BuildWhereExpression<T>(keys, cursor, forward));
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
    /// <typeparam name="T">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <typeparam name="TKey">
    /// The type of the parent key.
    /// </typeparam>
    /// <typeparam name="TProperty">
    /// The type of the property that is being paged.
    /// </typeparam>
    /// <returns>
    /// Returns a dictionary of pages for each parent.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// If the result is not a list or an enumerable.
    /// </exception>
    public static async ValueTask<Dictionary<PageKey<TKey>, Page<TProperty>>> ToBatchPageAsync<T, TKey, TProperty>(
        this IIncludableQueryable<T, IOrderedEnumerable<TProperty>> source,
        Func<T, TKey> keySelector,
        PagingArguments arguments,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (keySelector == null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        if (arguments.Last is not null && arguments.First is not null)
        {
            throw new ArgumentException(
                "You can specify either `first` or `last`, but not both as this can lead to unpredictable results.",
                nameof(arguments));
        }

        var rewriter = new BatchQueryRewriter<TProperty>(arguments);
        var expression = rewriter.Visit(source.Expression);
        var list = await source.Provider.CreateQuery<T>(expression).ToListAsync(cancellationToken);
        var result = new Dictionary<PageKey<TKey>, Page<TProperty>>();
        var requestedItems = int.MaxValue;

        if (arguments.First.HasValue)
        {
            requestedItems = arguments.First.Value;
        }

        if (arguments.Last.HasValue)
        {
            requestedItems = arguments.Last.Value;
        }

        foreach (var group in list)
        {
            var key = new PageKey<TKey>(keySelector(group), arguments);
            var builder = ImmutableArray.CreateBuilder<TProperty>();

            switch (rewriter.ResultProperty.GetValue(group))
            {
                case IReadOnlyList<TProperty> resultList:
                    builder.AddRange(resultList);

                    // if we over-fetched we remove the last item.
                    if (requestedItems < builder.Count)
                    {
                        builder.RemoveAt(builder.Count - 1);
                    }

                    result.Add(key, CreatePage(builder.ToImmutable(), arguments, rewriter.Keys, resultList.Count));
                    break;

                case IEnumerable<TProperty> resultEnumerable:
                    builder.AddRange(resultEnumerable);
                    var fetchCount = builder.Count;

                    // if we over-fetched we remove the last item.
                    if (requestedItems < fetchCount)
                    {
                        builder.RemoveAt(builder.Count - 1);
                    }

                    result.Add(key, CreatePage(builder.ToImmutable(), arguments, rewriter.Keys, fetchCount));
                    break;

                default:
                    throw new InvalidOperationException(
                        "The result must be a list or an enumerable.");
            }
        }

        return result;
    }

    public static async ValueTask<Dictionary<TKey, Page<TValue>>> ToBatchPageAsync<TKey, TValue>(
        this IQueryable<TValue> source,
        Expression<Func<TValue, TKey>> keySelector,
        PagingArguments arguments,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        CursorKey[] keys = ParseDataSetKeys(source);

        if (keys.Length == 0)
        {
            throw new ArgumentException(
                "In order to use cursor pagination, you must specify at least on key using the `OrderBy` method.",
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
        var selectExpression = BuildBatchSelectExpression<TKey, TValue>(arguments, keys, ref requestedCount);

        var map = new Dictionary<TKey, Page<TValue>>();
        var builder = ImmutableArray.CreateBuilder<>();
        int? totalCount = null;
        var fetchCount = 0;

        var x = source
            .GroupBy(keySelector)
            .Select(selectExpression)
            .ToQueryString();





        return default!;
    }

    private class Group<TKey, TValue>
    {
        public TKey Key { get; set; }

        public List<TValue> Items { get; set; }
    }

    private static Expression<Func<IGrouping<TK, TV>, Group<TK, TV>>> BuildBatchSelectExpression<TK, TV>(
        PagingArguments arguments,
        CursorKey[] keys,
        ref int requestedCount)
    {
        var forward = true;
        var group = Expression.Parameter(typeof(IGrouping<TK, TV>), "g");
        var groupKey = Expression.Property(group, "Key");
        Expression source = group;


        if (arguments.After is not null)
        {
            var cursor = CursorParser.Parse(arguments.After, keys);
            source = BuildBatchWhereExpression<TV>(source, keys, cursor, forward);
        }

        if (arguments.Before is not null)
        {
            var cursor = CursorParser.Parse(arguments.Before, keys);
            source = BuildBatchWhereExpression<TV>(source, keys, cursor, forward);
        }

        if (arguments.First is not null)
        {
            source = Expression.Call(
                typeof(Enumerable),
                "Take",
                [typeof(TV)],
                source,
                Expression.Constant(arguments.First.Value + 1));
            requestedCount = arguments.First.Value;
        }

        if (arguments.Last is not null)
        {
            source = Expression.Call(
                typeof(Enumerable),
                "Reverse",
                [typeof(TV)],
                source);
            source = Expression.Call(
                typeof(Enumerable),
                "Take",
                [typeof(TV)],
                source,
                Expression.Constant(arguments.Last.Value + 1));
            requestedCount = arguments.Last.Value;
        }

        source = Expression.Call(
            typeof(Enumerable),
            "ToList",
            [typeof(TV)],
            source);

        var groupType = typeof(Group<TK, TV>);
        var bindings = new MemberBinding[]
        {
            Expression.Bind(groupType.GetProperty(nameof(Group<TK, TV>.Key))!, groupKey),
            Expression.Bind(groupType.GetProperty(nameof(Group<TK, TV>.Items))!, source)
        };

        var createGroup = Expression.MemberInit(Expression.New(groupType), bindings);
        return Expression.Lambda<Func<IGrouping<TK, TV>, Group<TK, TV>>>(createGroup, group);
    }

    private static MethodCallExpression BuildBatchWhereExpression<T>(
        Expression enumerable,
        CursorKey[] keys,
        object?[] cursor,
        bool forward)
    {
        var cursorExpr = new Expression[cursor.Length];

        for (var i = 0; i < cursor.Length; i++)
        {
            cursorExpr[i] = CreateParameter(cursor[i], keys[i].Expression.ReturnType);
        }

        var handled = new List<CursorKey>();
        Expression? expression = null;

        var parameter = Expression.Parameter(typeof(T), "t");
        var zero = Expression.Constant(0);

        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            Expression? current = null;
            Expression keyExpr;

            for (var j = 0; j < handled.Count; j++)
            {
                var handledKey = handled[j];

                keyExpr =
                    Expression.Equal(
                        Expression.Call(
                            ReplaceParameter(handledKey.Expression, parameter),
                            handledKey.CompareMethod,
                            cursorExpr[j]),
                        zero);

                current = current is null
                    ? keyExpr
                    : Expression.AndAlso(current, keyExpr);
            }

            var greaterThan = forward
                ? key.Direction is CursorKeyDirection.Ascending
                : key.Direction is CursorKeyDirection.Descending;

            keyExpr =
                greaterThan
                    ? Expression.GreaterThan(
                        Expression.Call(
                            ReplaceParameter(key.Expression, parameter),
                            key.CompareMethod,
                            cursorExpr[i]),
                        zero)
                    : Expression.LessThan(
                        Expression.Call(
                            ReplaceParameter(key.Expression, parameter),
                            key.CompareMethod,
                            cursorExpr[i]),
                        zero);

            current = current is null
                ? keyExpr
                : Expression.AndAlso(current, keyExpr);
            expression = expression is null
                ? current
                : Expression.OrElse(expression, current);
            handled.Add(key);
        }

        return Expression.Call(
            typeof(Enumerable),
            "Where",
            [typeof(T)],
            enumerable,
            Expression.Lambda<Func<T, bool>>(expression!, parameter));
    }

    private static IQueryable<TValue> ApplyPaging<TValue>(
        IQueryable<TValue> query,
        PagingArguments arguments,
        CursorKey[] keys)
    {
        var forward = true;
        var requestedCount = int.MaxValue;

        if (arguments.After is not null)
        {
            var cursor = CursorParser.Parse(arguments.After, keys);
            query = query.Where(BuildWhereExpression<TValue>(keys, cursor, forward));
        }

        if (arguments.Before is not null)
        {
            var cursor = CursorParser.Parse(arguments.Before, keys);
            query = query.Where(BuildWhereExpression<TValue>(keys, cursor, forward));
        }

        if (arguments.First is not null)
        {
            query = query.Take(arguments.First.Value + 1);
            requestedCount = arguments.First.Value;
        }

        if (arguments.Last is not null)
        {
            query = query.Reverse().Take(arguments.Last.Value + 1);
            requestedCount = arguments.Last.Value;
        }

        return query;
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

        // if we required the last 5 items of a dataset and overfetch by 1
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

        return new Page<T>(items, hasNext, hasPrevious, item => CursorFormatter.Format(item, keys), totalCount);
    }

    private static CursorKey[] ParseDataSetKeys<T>(IQueryable<T> source)
    {
        var parser = new CursorKeyParser();
        parser.Visit(source.Expression);
        return parser.Keys.ToArray();
    }
}
