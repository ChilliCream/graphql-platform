using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using GreenDonut.Data.Cursors;
using GreenDonut.Data.Expressions;
using GreenDonut.Data.Internal;
using Microsoft.EntityFrameworkCore;
using static GreenDonut.Data.Expressions.ExpressionHelpers;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Data;

/// <summary>
/// Provides extension methods to page a queryable.
/// </summary>
public static class PagingQueryableExtensions
{
    private static readonly AsyncLocal<InterceptorHolder> s_interceptor = new();
    private static readonly ConcurrentDictionary<(Type, Type), Expression> s_countExpressionCache = new();

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
        var composition = PagingQueryComposer.Compose(source, arguments, includeTotalCount);
        var originalQuery = composition.OriginalQuery;
        var keys = composition.Keys;
        var cursor = composition.Cursor;
        var requestedCount = composition.RequestedPageSize;
        var isBackward = composition.Direction is PagingDirection.Backward;
        var totalCount = composition.TotalCount;
        arguments = composition.Arguments;
        includeTotalCount = composition.IncludeTotalCount;
        var isEndCursor = cursor?.IsEndCursor == true;

        if (isEndCursor)
        {
            var pagesBeforeLast = -cursor!.Offset!.Value;

            // An end cursor before the first page yields an empty page with a fresh total.
            if (pagesBeforeLast > 0
                && (int)Math.Ceiling(cursor.TotalCount!.Value / (double)requestedCount) - pagesBeforeLast < 1)
            {
                TryGetQueryInterceptor()?.OnBeforeExecute(originalQuery);
                var freshCount = await originalQuery.CountAsync(cancellationToken).ConfigureAwait(false);
                return CreateEndCursorPage<T>(
                    [],
                    keys,
                    hasNextPage: false,
                    hasPreviousPage: false,
                    1,
                    requestedCount,
                    freshCount);
            }
        }

        // An end cursor page takes exactly requestedCount rows; its flags come from the cursor total.
        var slicedQuery = composition.SlicedQuery.Take(isEndCursor ? requestedCount : requestedCount + 1);
        var pageQuery = composition.Selector is null
            ? slicedQuery
            : slicedQuery.Select(composition.Selector);

        var builder = ImmutableArray.CreateBuilder<T>();
        var fetchCount = 0;

        if (includeTotalCount)
        {
            var combinedQuery = pageQuery.Select(t => new { TotalCount = originalQuery.Count(), Item = t });

            TryGetQueryInterceptor()?.OnBeforeExecute(combinedQuery);

            await foreach (var item in combinedQuery.AsAsyncEnumerable()
                .WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                totalCount ??= item.TotalCount;
                fetchCount++;

                builder.Add(item.Item);

                if (fetchCount > requestedCount)
                {
                    break;
                }
            }
        }
        else
        {
            TryGetQueryInterceptor()?.OnBeforeExecute(pageQuery);

            await foreach (var item in pageQuery.AsAsyncEnumerable()
                .WithCancellation(cancellationToken).ConfigureAwait(false))
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
            if (includeTotalCount)
            {
                TryGetQueryInterceptor()?.OnBeforeExecute(originalQuery);
                totalCount = await originalQuery.CountAsync(cancellationToken).ConfigureAwait(false);
            }

            if (isEndCursor)
            {
                return CreateEndCursorPage<T>(
                    [],
                    keys,
                    hasNextPage: false,
                    hasPreviousPage: false,
                    1,
                    requestedCount,
                    totalCount ?? 0);
            }

            return Page<T>.Create([], hasNextPage: false, hasPreviousPage: false, _ => string.Empty, totalCount);
        }

        if (isBackward)
        {
            builder.Reverse();
        }

        if (isEndCursor)
        {
            var pagesBeforeLast = -cursor!.Offset!.Value;
            var effectiveTotal = totalCount!.Value;
            var items = builder.ToImmutable();

            if (pagesBeforeLast == 0)
            {
                var remainder = effectiveTotal % requestedCount;

                if (effectiveTotal > requestedCount && remainder != 0)
                {
                    var trim = requestedCount - remainder;
                    items = items[trim..];
                }
            }

            // The last page's index uses the fresh total; earlier pages use the cursor's total.
            var indexTotal = pagesBeforeLast == 0 ? effectiveTotal : cursor.TotalCount!.Value;
            var index = (int)Math.Ceiling(indexTotal / (double)requestedCount) - pagesBeforeLast;

            return CreateEndCursorPage(
                Page<T>.ToEntries(items),
                keys,
                hasNextPage: pagesBeforeLast > 0,
                hasPreviousPage: index > 1,
                index,
                requestedCount,
                effectiveTotal,
                items);
        }

        if (builder.Count > requestedCount)
        {
            builder.RemoveAt(isBackward ? 0 : requestedCount);
        }

        var pageItems = builder.ToImmutable();
        var pageIndex = CreateIndex(arguments, cursor, totalCount);
        return CreateValueCursorPage(
            Page<T>.ToEntries(pageItems),
            arguments,
            keys,
            fetchCount,
            pageIndex,
            requestedCount,
            totalCount,
            pageItems);
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
    /// <returns>
    /// A dictionary mapping each parent key to its page of results.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified.
    /// </exception>
    public static ValueTask<Dictionary<TKey, Page<TValue>>> ToBatchPageAsync<TKey, TValue>(
        this IQueryable<TValue> source,
        Expression<Func<TValue, TKey>> keySelector,
        PagingArguments arguments,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => ToBatchPageAsync<TKey, TValue, TValue>(
            source,
            keySelector,
            null,
            arguments,
            includeTotalCount: arguments.IncludeTotalCount,
            cancellationToken);

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
    /// <param name="includeTotalCount">
    /// If set to <c>true</c> the total count will be included in the result.
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
    /// <returns>
    /// A dictionary mapping each parent key to its page of results.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified.
    /// </exception>
    public static ValueTask<Dictionary<TKey, Page<TValue>>> ToBatchPageAsync<TKey, TValue>(
        this IQueryable<TValue> source,
        Expression<Func<TValue, TKey>> keySelector,
        PagingArguments arguments,
        bool includeTotalCount,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => ToBatchPageAsync<TKey, TValue, TValue>(
            source,
            keySelector,
            null,
            arguments,
            includeTotalCount: includeTotalCount,
            cancellationToken);

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
    /// The type of the value selected from the items in the queryable.
    /// </typeparam>
    /// <typeparam name="TElement">
    /// The type of the source elements from which keys and values are projected.
    /// </typeparam>
    /// <returns>
    /// A dictionary mapping each parent key to its page of results.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified.
    /// </exception>
    public static ValueTask<Dictionary<TKey, Page<TValue>>> ToBatchPageAsync<TKey, TValue, TElement>(
        this IQueryable<TElement> source,
        Expression<Func<TElement, TKey>> keySelector,
        Func<TElement, TValue> valueSelector,
        PagingArguments arguments,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => ToBatchPageAsync(
            source,
            keySelector,
            valueSelector,
            arguments,
            includeTotalCount: arguments.IncludeTotalCount,
            cancellationToken);

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
    /// <param name="includeTotalCount">
    /// If set to <c>true</c> the total count will be included in the result.
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
    /// The type of the source elements from which keys and values are projected.
    /// </typeparam>
    /// <returns>
    /// A dictionary mapping each parent key to its page of results.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified, if <c>first</c> or <c>last</c> is
    /// given and not greater than zero, or if an end cursor in <c>before</c> has a skip that
    /// does not fit into an <see cref="int"/>.
    /// </exception>
    public static async ValueTask<Dictionary<TKey, Page<TValue>>> ToBatchPageAsync<TKey, TValue, TElement>(
        this IQueryable<TElement> source,
        Expression<Func<TElement, TKey>> keySelector,
        Func<TElement, TValue>? valueSelector,
        PagingArguments arguments,
        bool includeTotalCount,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        source = QueryHelpers.EnsureOrderPropsAreSelected(source);

        var selector = QueryHelpers.ExtractCurrentSelector(source);

        // The selector is removed before grouping and re-applied afterwards.
        if (selector is not null)
        {
            source = QueryHelpers.RemoveSelector(source);
        }

        var keys = PagingQueryComposer.ParseDataSetKeys(source);

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

        PagingQueryComposer.ValidateRequestedCount(arguments);

        if (valueSelector is null && !typeof(TValue).IsAssignableFrom(typeof(TElement)))
        {
            throw new ArgumentException(
                "If no value selector is provided, the source element type must be assignable to the value type.",
                nameof(valueSelector));
        }

        if (arguments.EnableRelativeCursors
            && string.IsNullOrEmpty(arguments.After)
            && string.IsNullOrEmpty(arguments.Before))
        {
            includeTotalCount = true;
        }

        // An end cursor page always needs each key's own total.
        if (!string.IsNullOrEmpty(arguments.Before))
        {
            var beforeCursor = CursorParser.Parse(arguments.Before, keys);

            if (beforeCursor.IsEndCursor)
            {
                if (arguments.After is not null)
                {
                    throw ThrowHelper.PagingArguments_EndCursorBeforeCombinedWithAfter();
                }

                if (arguments.Last is not null)
                {
                    PagingQueryComposer.GetBatchEndCursorSkip(-beforeCursor.Offset!.Value, arguments.Last.Value);
                }

                includeTotalCount = true;
            }
        }

        source = QueryHelpers.EnsureGroupPropsAreSelected(source, keySelector);

        // The ordering is moved into the select expression so GroupBy keeps it.
        var ordering = ExtractAndRemoveOrder(source.Expression);

        Dictionary<TKey, int>? counts = null;
        if (includeTotalCount)
        {
            counts = await GetBatchCountsAsync(source, keySelector, cancellationToken);
        }

        var map = new Dictionary<TKey, Page<TValue>>();

        var forward = arguments.Last is null;
        var requestedCount = int.MaxValue;
        var batchExpression =
            BuildBatchExpression<TKey, TElement>(
                arguments,
                keys,
                ordering.OrderExpressions,
                ordering.OrderMethods,
                forward,
                selector,
                ref requestedCount);

        source = source.Provider.CreateQuery<TElement>(ordering.Expression);

        TryGetQueryInterceptor()?.OnBeforeExecute(source.GroupBy(keySelector).Select(batchExpression.SelectExpression));

        await foreach (var item in source
            .GroupBy(keySelector)
            .Select(batchExpression.SelectExpression)
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            var totalCount = counts?.GetValueOrDefault(item.Key) ?? batchExpression.Cursor?.TotalCount;
            var isEndCursor = batchExpression.Cursor?.IsEndCursor == true;
            var pagesBeforeLast = isEndCursor ? -batchExpression.Cursor!.Offset!.Value : 0;

            // An end cursor page aligns to each key's own total, never the cursor's cached one.
            var pageIndex = isEndCursor
                ? (int)Math.Ceiling(totalCount!.Value / (double)requestedCount) - pagesBeforeLast
                : CreateIndex(arguments, batchExpression.Cursor, totalCount);

            if (isEndCursor && pageIndex < 1)
            {
                map.Add(
                    item.Key,
                    CreateEndCursorPage<TValue>(
                        [],
                        keys,
                        hasNextPage: false,
                        hasPreviousPage: false,
                        1,
                        requestedCount,
                        totalCount!.Value));
                continue;
            }

            if (item.Items.Count == 0)
            {
                var page = isEndCursor
                    ? CreateEndCursorPage<TValue>(
                        [],
                        keys,
                        hasNextPage: false,
                        hasPreviousPage: false,
                        1,
                        requestedCount,
                        totalCount ?? 0)
                    : Page<TValue>.Create(
                        [],
                        hasNextPage: false,
                        hasPreviousPage: false,
                        static _ => string.Empty,
                        totalCount);
                map.Add(item.Key, page);
                continue;
            }

            var itemOffset = 0;
            var itemCount = requestedCount > item.Items.Count ? item.Items.Count : requestedCount;

            if (isEndCursor)
            {
                var effectiveTotal = totalCount!.Value;

                if (pagesBeforeLast == 0)
                {
                    var remainder = effectiveTotal % requestedCount;

                    if (effectiveTotal > requestedCount && remainder != 0)
                    {
                        itemCount -= requestedCount - remainder;
                    }
                }
                else
                {
                    // This key's exact page sits inside its window at its own remainder, never
                    // the cursor's cached one.
                    itemOffset = effectiveTotal % requestedCount == 0
                        ? requestedCount
                        : effectiveTotal % requestedCount;
                    itemCount = requestedCount;
                }
            }

            if (valueSelector is not null)
            {
                var entryBuilder = ImmutableArray.CreateBuilder<PageEntry<TValue>>(itemCount);
                var elementBuilder = ImmutableArray.CreateBuilder<TElement>(itemCount);

                if (batchExpression.IsBackward)
                {
                    for (var i = itemCount - 1; i >= 0; i--)
                    {
                        var element = item.Items[itemOffset + i];
                        entryBuilder.Add(new PageEntry<TValue>(valueSelector(element), entryBuilder.Count));
                        elementBuilder.Add(element);
                    }
                }
                else
                {
                    for (var i = 0; i < itemCount; i++)
                    {
                        var element = item.Items[itemOffset + i];
                        entryBuilder.Add(new PageEntry<TValue>(valueSelector(element), entryBuilder.Count));
                        elementBuilder.Add(element);
                    }
                }

                var page = isEndCursor
                    ? CreateEndCursorElementPage<TElement, TValue>(
                        entryBuilder.ToImmutable(),
                        elementBuilder.ToImmutable(),
                        keys,
                        hasNextPage: pagesBeforeLast > 0,
                        hasPreviousPage: pageIndex > 1,
                        pageIndex!.Value,
                        requestedCount,
                        totalCount!.Value)
                    : CreateElementCursorPage(
                        entryBuilder.ToImmutable(),
                        elementBuilder.ToImmutable(),
                        arguments,
                        keys,
                        item.Items.Count,
                        pageIndex,
                        requestedCount,
                        totalCount);
                map.Add(item.Key, page);
            }
            else
            {
                var entryBuilder = ImmutableArray.CreateBuilder<PageEntry<TValue>>(itemCount);

                if (batchExpression.IsBackward)
                {
                    for (var i = itemCount - 1; i >= 0; i--)
                    {
                        entryBuilder.Add(
                            new PageEntry<TValue>((TValue)(object)item.Items[itemOffset + i]!, entryBuilder.Count));
                    }
                }
                else
                {
                    for (var i = 0; i < itemCount; i++)
                    {
                        entryBuilder.Add(
                            new PageEntry<TValue>((TValue)(object)item.Items[itemOffset + i]!, entryBuilder.Count));
                    }
                }

                var page = isEndCursor
                    ? CreateEndCursorPage<TValue>(
                        entryBuilder.ToImmutable(),
                        keys,
                        hasNextPage: pagesBeforeLast > 0,
                        hasPreviousPage: pageIndex > 1,
                        pageIndex!.Value,
                        requestedCount,
                        totalCount!.Value)
                    : CreateValueCursorPage(
                        entryBuilder.ToImmutable(),
                        arguments,
                        keys,
                        item.Items.Count,
                        pageIndex,
                        requestedCount,
                        totalCount);
                map.Add(item.Key, page);
            }
        }

        return map;
    }

    private static async Task<Dictionary<TKey, int>> GetBatchCountsAsync<TElement, TKey>(
        IQueryable<TElement> source,
        Expression<Func<TElement, TKey>> keySelector,
        CancellationToken cancellationToken)
        where TKey : notnull
    {
        var query = source
            .GroupBy(keySelector)
            .Select(GetOrCreateCountSelector<TElement, TKey>());

        TryGetQueryInterceptor()?.OnBeforeExecute(query);

        return await query.ToDictionaryAsync(t => t.Key, t => t.Count, cancellationToken);
    }

    private static Expression<Func<IGrouping<TKey, TElement>, CountResult<TKey>>> GetOrCreateCountSelector<TElement, TKey>()
    {
        return (Expression<Func<IGrouping<TKey, TElement>, CountResult<TKey>>>)
            s_countExpressionCache.GetOrAdd(
                (typeof(TKey), typeof(TElement)),
                static _ =>
                {
                    var groupingType = typeof(IGrouping<,>).MakeGenericType(typeof(TKey), typeof(TElement));
                    var param = Expression.Parameter(groupingType, "g");
                    var keyProperty = Expression.Property(param, nameof(IGrouping<TKey, TElement>.Key));
                    var countMethod = typeof(Enumerable)
                        .GetMethods(BindingFlags.Static | BindingFlags.Public)
                        .First(m => m.Name == nameof(Enumerable.Count) && m.GetParameters().Length == 1)
                        .MakeGenericMethod(typeof(TElement));
                    var countCall = Expression.Call(countMethod, param);

                    var resultCtor = typeof(CountResult<TKey>).GetConstructor(Type.EmptyTypes)!;
                    var newExpr = Expression.New(resultCtor);

                    var bindings = new List<MemberBinding>
                    {
                        Expression.Bind(
                            typeof(CountResult<TKey>).GetProperty(nameof(CountResult<TKey>.Key))!,
                            keyProperty),
                        Expression.Bind(
                            typeof(CountResult<TKey>).GetProperty(nameof(CountResult<TKey>.Count))!,
                            countCall)
                    };

                    var body = Expression.MemberInit(newExpr, bindings);
                    return Expression.Lambda<Func<IGrouping<TKey, TElement>, CountResult<TKey>>>(body, param);
                });
    }

    private class CountResult<TKey>
    {
        public TKey Key { get; set; } = default!;
        public int Count { get; set; }
    }

    internal static Page<T> CreateValueCursorPage<T>(
        ImmutableArray<PageEntry<T>> entries,
        PagingArguments arguments,
        CursorKey[] keys,
        int fetchCount,
        int? index,
        int? requestedPageSize,
        int? totalCount,
        ImmutableArray<T> items = default)
    {
        var (hasNext, hasPrevious) = CreatePageFlags(arguments, fetchCount);

        if (arguments.EnableRelativeCursors && totalCount is not null && requestedPageSize is not null)
        {
            return new ValueCursorPage<T>(
                entries,
                hasNext,
                hasPrevious,
                entry => CursorFormatter.Format(entry.Node, keys, new CursorPageInfo(entry.Offset, entry.PageIndex, entry.TotalCount)),
                index ?? 1,
                requestedPageSize.Value,
                totalCount.Value,
                items);
        }

        return new ValueCursorPage<T>(
            entries,
            hasNext,
            hasPrevious,
            item => CursorFormatter.Format(item, keys),
            totalCount,
            items);
    }

    internal static Page<TValue> CreateElementCursorPage<TElement, TValue>(
        ImmutableArray<PageEntry<TValue>> entries,
        ImmutableArray<TElement> elements,
        PagingArguments arguments,
        CursorKey[] keys,
        int fetchCount,
        int? index,
        int? requestedPageSize,
        int? totalCount)
    {
        var (hasNext, hasPrevious) = CreatePageFlags(arguments, fetchCount);

        if (arguments.EnableRelativeCursors && totalCount is not null && requestedPageSize is not null)
        {
            return new ElementCursorPage<TElement, TValue>(
                entries,
                elements,
                hasNext,
                hasPrevious,
                entry => CursorFormatter.Format(entry.Node, keys, new CursorPageInfo(entry.Offset, entry.PageIndex, entry.TotalCount)),
                index ?? 1,
                requestedPageSize.Value,
                totalCount.Value);
        }

        return new ElementCursorPage<TElement, TValue>(
            entries,
            elements,
            hasNext,
            hasPrevious,
            item => CursorFormatter.Format(item, keys),
            totalCount: totalCount);
    }

    internal static Page<T> CreateEndCursorPage<T>(
        ImmutableArray<PageEntry<T>> entries,
        CursorKey[] keys,
        bool hasNextPage,
        bool hasPreviousPage,
        int index,
        int requestedPageSize,
        int totalCount,
        ImmutableArray<T> items = default)
        => new ValueCursorPage<T>(
            entries,
            hasNextPage,
            hasPreviousPage,
            entry => CursorFormatter.Format(entry.Node, keys, new CursorPageInfo(entry.Offset, entry.PageIndex, entry.TotalCount)),
            index,
            requestedPageSize,
            totalCount,
            items);

    internal static Page<TValue> CreateEndCursorElementPage<TElement, TValue>(
        ImmutableArray<PageEntry<TValue>> entries,
        ImmutableArray<TElement> elements,
        CursorKey[] keys,
        bool hasNextPage,
        bool hasPreviousPage,
        int index,
        int requestedPageSize,
        int totalCount)
        => new ElementCursorPage<TElement, TValue>(
            entries,
            elements,
            hasNextPage,
            hasPreviousPage,
            entry => CursorFormatter.Format(entry.Node, keys, new CursorPageInfo(entry.Offset, entry.PageIndex, entry.TotalCount)),
            index,
            requestedPageSize,
            totalCount);

    internal static (bool HasNext, bool HasPrevious) CreatePageFlags(
        PagingArguments arguments,
        int fetchCount)
    {
        var hasPrevious = false;
        var hasNext = false;

        // A skip with any fetched items means there is a previous page.
        if (arguments.After is not null && fetchCount > 0)
        {
            hasPrevious = true;
        }

        // Over-fetching beyond `last` means there is a previous page.
        if (arguments.Last is not null && fetchCount > arguments.Last)
        {
            hasPrevious = true;
        }

        // Over-fetching beyond `first` means there is a next page.
        if (arguments.First is not null && fetchCount > arguments.First)
        {
            hasNext = true;
        }

        // A `before` cursor means there is at least one more item.
        if (arguments.Before is not null)
        {
            hasNext = true;
        }

        return (hasNext, hasPrevious);
    }

    internal static int? CreateIndex(PagingArguments arguments, Cursor? cursor, int? totalCount)
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

    private sealed class InterceptorHolder
    {
        public PagingQueryInterceptor? Interceptor { get; set; }
    }

    internal static PagingQueryInterceptor? TryGetQueryInterceptor()
        => s_interceptor.Value?.Interceptor;

    internal static void SetQueryInterceptor(PagingQueryInterceptor pagingQueryInterceptor)
    {
        s_interceptor.Value ??= new InterceptorHolder();
        s_interceptor.Value.Interceptor = pagingQueryInterceptor;
    }

    internal static void ClearQueryInterceptor()
    {
        s_interceptor.Value?.Interceptor = null;
    }
}
