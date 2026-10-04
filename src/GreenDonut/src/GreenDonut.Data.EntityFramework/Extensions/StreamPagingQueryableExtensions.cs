using System.Linq.Expressions;
using GreenDonut.Data.Cursors;
using GreenDonut.Data.Expressions;
using GreenDonut.Data.Internal;
using Microsoft.EntityFrameworkCore;
using static GreenDonut.Data.Expressions.ExpressionHelpers;

// ReSharper disable once CheckNamespace
namespace GreenDonut.Data;

/// <summary>
/// Provides extension methods to page a queryable into a <see cref="StreamPage{T}"/> whose items
/// and page info stream from the underlying source.
/// </summary>
public static class StreamPagingQueryableExtensions
{
    /// <summary>
    /// Executes a query with paging and returns the selected page, streaming its items as they
    /// arrive from the database.
    /// </summary>
    /// <param name="source">
    /// The queryable to be paged.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <param name="lifetime">
    /// A resource, such as a database context, that must stay alive for as long as the page is
    /// being read. It is disposed once the page completes or is disposed, or immediately if this
    /// call does not end up streaming any rows.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token that governs creating the page.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns a streaming page of items.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified, if <c>first</c> or <c>last</c> is
    /// given and not greater than zero, or if a relative cursor's offset does not fit into an
    /// <see cref="int"/>.
    /// </exception>
    public static ValueTask<StreamPage<T>> ToStreamPageAsync<T>(
        this IQueryable<T> source,
        PagingArguments arguments,
        IAsyncDisposable? lifetime = null,
        CancellationToken cancellationToken = default)
        => source.ToStreamPageAsync(arguments, arguments.IncludeTotalCount, lifetime, cancellationToken);

    /// <summary>
    /// Executes a query with paging and returns the selected page, streaming its items as they
    /// arrive from the database.
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
    /// <param name="lifetime">
    /// A resource, such as a database context, that must stay alive for as long as the page is
    /// being read. It is disposed once the page completes or is disposed, or immediately if this
    /// call does not end up streaming any rows.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token that governs creating the page.
    /// </param>
    /// <typeparam name="T">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns>
    /// Returns a streaming page of items.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified, if <c>first</c> or <c>last</c> is
    /// given and not greater than zero, if a relative cursor's offset does not fit into an
    /// <see cref="int"/>, or if <see cref="PagingArguments.IncludeItems"/> is <c>false</c>
    /// while <paramref name="includeTotalCount"/> is also <c>false</c>.
    /// </exception>
    public static async ValueTask<StreamPage<T>> ToStreamPageAsync<T>(
        this IQueryable<T> source,
        PagingArguments arguments,
        bool includeTotalCount,
        IAsyncDisposable? lifetime = null,
        CancellationToken cancellationToken = default)
    {
        PagingQueryComposition<T> composition;
        IQueryable<T> originalQuery;
        CursorKey[] keys;
        Cursor? cursor;
        int requestedCount;
        bool isBackward;

        try
        {
            composition = PagingQueryComposer.Compose(source, arguments, includeTotalCount);
            originalQuery = composition.OriginalQuery;
            keys = composition.Keys;
            cursor = composition.Cursor;
            requestedCount = composition.RequestedPageSize;
            isBackward = composition.Direction is PagingDirection.Backward;
            arguments = composition.Arguments;
            includeTotalCount = composition.IncludeTotalCount;
        }
        catch (Exception compositionException)
        {
            await ReleaseLifetimeOnFaultAsync(compositionException, lifetime).ConfigureAwait(false);

            throw;
        }

        var relative = arguments.EnableRelativeCursors;

        // Whether a backward page's flags come from index arithmetic instead of the has-more probe.
        var usesRelativeBackwardFlags = relative && cursor?.IsRelative != false;

        // count-only: no row query ever runs, so no lifetime is ever handed to a page.
        if (!arguments.IncludeItems)
        {
            if (!includeTotalCount)
            {
                await DisposeLifetimeAsync(lifetime).ConfigureAwait(false);

                throw ThrowHelper.PagingArguments_IncludeItemsFalseRequiresTotalCount();
            }

            PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(originalQuery);
            var countOnlyTotal = await RunCountAsync(originalQuery, lifetime, cancellationToken).ConfigureAwait(false);
            await DisposeLifetimeAsync(lifetime).ConfigureAwait(false);

            int? countOnlyIndex = relative
                ? PagingQueryableExtensions.CreateIndex(arguments, cursor, countOnlyTotal) ?? 1
                : null;
            return await CreateResolvedPageAsync<T>(
                keys,
                hasNextPage: false,
                hasPreviousPage: false,
                countOnlyIndex,
                requestedCount,
                countOnlyTotal,
                cancellationToken)
                .ConfigureAwait(false);
        }

        var isEndCursor = cursor?.IsEndCursor == true;

        if (isEndCursor)
        {
            var pagesBeforeLast = -cursor!.Offset!.Value;

            // An end cursor that points before the first page yields an empty page with a freshly counted total.
            if (pagesBeforeLast > 0
                && (int)Math.Ceiling(cursor.TotalCount!.Value / (double)requestedCount) - pagesBeforeLast < 1)
            {
                PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(originalQuery);
                var freshCount = await RunCountAsync(originalQuery, lifetime, cancellationToken).ConfigureAwait(false);
                await DisposeLifetimeAsync(lifetime).ConfigureAwait(false);

                return await CreateResolvedPageAsync<T>(
                    keys,
                    hasNextPage: false,
                    hasPreviousPage: false,
                    1,
                    requestedCount,
                    freshCount,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // A plain backward page carries its previous-page check along with the rows.
        var needsHasMore = isBackward && !isEndCursor && !usesRelativeBackwardFlags && arguments.After is null;

#if NET8_0
        // The total count fetched for this page, reused by the empty-row branch below.
        int? totalCountValue = null;
#endif

        IAsyncEnumerator<StreamRow<T>> enumerator;

        try
        {
            IQueryable<T> pageQuery;
            IQueryable<T>? hasMoreSource = null;

            if (isBackward)
            {
                // Backward pages take exactly the requested count from the inverted order and
                // re-apply the original order.
                var taken = composition.SlicedQuery.Take(requestedCount);
                pageQuery = OriginalOrderReapplier.Reapply(taken, keys);

                if (needsHasMore)
                {
                    hasMoreSource = composition.SlicedQuery.Skip(requestedCount);
                }
            }
            else
            {
                // Forward pages read one extra row to answer HasNextPage; it is never yielded.
                pageQuery = composition.SlicedQuery.Take(requestedCount + 1);
            }

            if (composition.Selector is not null)
            {
                pageQuery = pageQuery.Select(composition.Selector);
            }

            IQueryable<StreamRow<T>> rowQuery;

#if NET8_0
            // On EF Core 8, HasMore and the total count are fetched here explicitly before the
            // row query runs, then carried into it as plain parameters.
            bool? hasMoreValue = null;

            if (hasMoreSource is { } probeQuery)
            {
                PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(probeQuery);
                hasMoreValue = await probeQuery.AnyAsync(cancellationToken).ConfigureAwait(false);
            }

            if (includeTotalCount)
            {
                PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(originalQuery);
                totalCountValue = await originalQuery.CountAsync(cancellationToken).ConfigureAwait(false);
            }

            rowQuery = BuildRowQuery(pageQuery, hasMoreValue, totalCountValue);
#else
            rowQuery = BuildRowQuery(pageQuery, originalQuery, hasMoreSource, includeTotalCount);
#endif

            PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(rowQuery);

            enumerator = rowQuery.AsAsyncEnumerable().GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception buildException)
        {
            await ReleaseLifetimeOnFaultAsync(buildException, lifetime).ConfigureAwait(false);

            throw;
        }

        bool hasFirstRow;

        try
        {
            hasFirstRow = await enumerator.MoveNextAsync().ConfigureAwait(false);
        }
        catch (Exception primingException)
        {
            // A disposal failure while cleaning up is attached to the priming failure instead of
            // replacing it.
            try
            {
                await OrderedDisposal.ReleaseAsync(
                    enumerator.DisposeAsync,
                    lifetime is null ? null : lifetime.DisposeAsync)
                    .ConfigureAwait(false);
            }
            catch (Exception releaseException)
            {
                OrderedDisposal.Attach(primingException, releaseException);
            }

            throw;
        }

        if (!hasFirstRow)
        {
            // An empty row query carries no inlined count; the total count is reused from an
            // earlier fetch on .NET 8, or fetched separately on every other target framework.
            try
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception disposeException)
            {
                // The lifetime is still released; a failure releasing it is attached to the
                // enumerator's disposal failure instead of replacing it.
                try
                {
                    await DisposeLifetimeAsync(lifetime).ConfigureAwait(false);
                }
                catch (Exception lifetimeException)
                {
                    OrderedDisposal.Attach(disposeException, lifetimeException);
                }

                throw;
            }

#if NET8_0
            var emptyTotalCount = totalCountValue ?? composition.TotalCount;
#else
            var emptyTotalCount = composition.TotalCount;

            if (includeTotalCount)
            {
                PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(originalQuery);
                emptyTotalCount = await RunCountAsync(originalQuery, lifetime, cancellationToken).ConfigureAwait(false);
            }
#endif

            await DisposeLifetimeAsync(lifetime).ConfigureAwait(false);

            if (isEndCursor)
            {
                return await CreateResolvedPageAsync<T>(
                    keys,
                    hasNextPage: false,
                    hasPreviousPage: false,
                    1,
                    requestedCount,
                    emptyTotalCount ?? 0,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            // An empty page never carries an index.
            return await CreateResolvedPageAsync<T>(
                keys,
                hasNextPage: false,
                hasPreviousPage: false,
                null,
                requestedCount,
                emptyTotalCount,
                cancellationToken)
                .ConfigureAwait(false);
        }

        var firstRow = enumerator.Current;
        var totalCount = includeTotalCount ? firstRow.TotalCount : composition.TotalCount;

        int? index;
        bool? hasNextPage;
        bool? hasPreviousPage;
        var trailingSentinel = false;
        var skipFront = 0;

        if (isEndCursor)
        {
            var pagesBeforeLast = -cursor!.Offset!.Value;
            var effectiveTotal = totalCount!.Value;

            // The last page derives its index from the fresh total; earlier pages reuse the cursor's total.
            var indexTotal = pagesBeforeLast == 0 ? effectiveTotal : cursor.TotalCount!.Value;

            index = (int)Math.Ceiling(indexTotal / (double)requestedCount) - pagesBeforeLast;
            hasNextPage = pagesBeforeLast > 0;
            hasPreviousPage = index > 1;
            totalCount = effectiveTotal;

            if (pagesBeforeLast == 0 && effectiveTotal > requestedCount && effectiveTotal % requestedCount != 0)
            {
                skipFront = requestedCount - effectiveTotal % requestedCount;
            }
        }
        else if (isBackward && !usesRelativeBackwardFlags)
        {
            // A plain backward page: HasNextPage follows the before cursor, and HasPreviousPage
            // follows the probe or an after cursor.
            index = relative && totalCount is not null
                ? PagingQueryableExtensions.CreateIndex(arguments, cursor, totalCount) ?? 1
                : null;
            hasNextPage = arguments.Before is not null;
            hasPreviousPage = arguments.After is not null || (firstRow.HasMore ?? false);
        }
        else
        {
            // A relative page always reports an index; the entry-point page defaults to 1.
            index = relative && totalCount is not null
                ? PagingQueryableExtensions.CreateIndex(arguments, cursor, totalCount) ?? 1
                : null;

            if (isBackward)
            {
                hasPreviousPage = index is > 1;
                hasNextPage = index is not null && totalCount is not null
                    && index < Math.Ceiling(totalCount.Value / (double)requestedCount);
            }
            else
            {
                trailingSentinel = true;
                hasNextPage = arguments.Before is not null ? true : null;
                hasPreviousPage = arguments.After is not null;
            }
        }

        var pump = new StreamPagePump<T>(enumerator, pageCount: 1, lifetime, firstRow);
        var createCursor = CreateCursorFactory<T>(keys, relativeShaped: index is not null);
        var definition = new StreamPageDefinition<T>(
            RequestedCount: requestedCount,
            Forward: !isBackward,
            TrailingSentinel: trailingSentinel,
            SkipFront: skipFront,
            SkipFrontFromCount: null,
            Index: index,
            RequestedSize: requestedCount,
            TotalCount: totalCount,
            HasNextPage: hasNextPage,
            HasPreviousPage: hasPreviousPage,
            FlagsFromFirstRow: null);

        return await ValueCursorStreamPage<T>.CreatePrimedAsync(
            pump,
            definition,
            createCursor,
            cancellationToken)
            .ConfigureAwait(false);
    }

#if NET8_0
    // Builds the row query for EF Core 8, where HasMore and TotalCount arrive as already-known
    // parameters instead of live subqueries.
    private static IQueryable<StreamRow<T>> BuildRowQuery<T>(
        IQueryable<T> pageQuery,
        bool? hasMore,
        int? totalCount)
    {
        if (hasMore is { } hasMoreValue)
        {
            return totalCount is { } totalWithHasMore
                ? pageQuery.Select(t => new StreamRow<T>
                {
                    Item = t,
                    TotalCount = totalWithHasMore,
                    HasMore = hasMoreValue
                })
                : pageQuery.Select(t => new StreamRow<T> { Item = t, HasMore = hasMoreValue });
        }

        return totalCount is { } total
            ? pageQuery.Select(t => new StreamRow<T> { Item = t, TotalCount = total })
            : pageQuery.Select(t => new StreamRow<T> { Item = t });
    }
#else
    private static IQueryable<StreamRow<T>> BuildRowQuery<T>(
        IQueryable<T> pageQuery,
        IQueryable<T> originalQuery,
        IQueryable<T>? hasMoreSource,
        bool includeTotalCount)
    {
        if (hasMoreSource is { } hasMoreQuery)
        {
            return includeTotalCount
                ? pageQuery.Select(t => new StreamRow<T>
                {
                    Item = t,
                    TotalCount = originalQuery.Count(),
                    HasMore = hasMoreQuery.Any()
                })
                : pageQuery.Select(t => new StreamRow<T> { Item = t, HasMore = hasMoreQuery.Any() });
        }

        return includeTotalCount
            ? pageQuery.Select(t => new StreamRow<T> { Item = t, TotalCount = originalQuery.Count() })
            : pageQuery.Select(t => new StreamRow<T> { Item = t });
    }
#endif

    private static async ValueTask<StreamPage<T>> CreateResolvedPageAsync<T>(
        CursorKey[] keys,
        bool hasNextPage,
        bool hasPreviousPage,
        int? index,
        int requestedCount,
        int? totalCount,
        CancellationToken cancellationToken)
    {
        var createCursor = CreateCursorFactory<T>(keys, relativeShaped: index is not null);
        var definition = new StreamPageDefinition<T>(
            RequestedCount: requestedCount,
            Forward: true,
            TrailingSentinel: false,
            SkipFront: 0,
            SkipFrontFromCount: null,
            Index: index,
            RequestedSize: requestedCount,
            TotalCount: totalCount,
            HasNextPage: hasNextPage,
            HasPreviousPage: hasPreviousPage,
            FlagsFromFirstRow: null);

        return await ValueCursorStreamPage<T>.CreatePrimedAsync(
            pump: null,
            definition,
            createCursor,
            cancellationToken)
            .ConfigureAwait(false);
    }

    private static Func<EdgeEntry<T>, string> CreateCursorFactory<T>(CursorKey[] keys, bool relativeShaped)
        => relativeShaped
            ? entry => CursorFormatter.Format(
                entry.Node,
                keys,
                new CursorPageInfo(entry.Offset, entry.PageIndex, entry.TotalCount))
            : entry => CursorFormatter.Format(entry.Node, keys);

    private static ValueTask DisposeLifetimeAsync(IAsyncDisposable? lifetime)
        => lifetime?.DisposeAsync() ?? default;

    // Releases a caller-supplied lifetime on any fault before the pump takes ownership of it; a
    // disposal failure during cleanup is attached to the original fault instead of replacing it.
    private static async ValueTask ReleaseLifetimeOnFaultAsync(Exception fault, IAsyncDisposable? lifetime)
    {
        try
        {
            await DisposeLifetimeAsync(lifetime).ConfigureAwait(false);
        }
        catch (Exception lifetimeException)
        {
            OrderedDisposal.Attach(fault, lifetimeException);
        }
    }

    // A failing count still owns the lifetime it was handed; a disposal failure while cleaning up
    // is attached to the count's own fault instead of replacing it.
    private static async ValueTask<int> RunCountAsync<T>(
        IQueryable<T> query,
        IAsyncDisposable? lifetime,
        CancellationToken cancellationToken)
    {
        try
        {
            return await query.CountAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception countException)
        {
            await ReleaseLifetimeOnFaultAsync(countException, lifetime).ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>
    /// Executes a batch query with paging and returns the selected streaming pages for each
    /// parent, from one flat, key-ordered query.
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
    /// <param name="lifetime">
    /// A resource, such as a database context, that must stay alive for as long as any of the
    /// returned pages is being read. It is disposed once every page has completed or been
    /// disposed, or immediately if this call does not end up streaming any rows.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token that governs creating the pages.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of the parent key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns>
    /// A dictionary mapping each requested key to its streaming page of results.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified.
    /// </exception>
    public static ValueTask<Dictionary<TKey, StreamPage<TValue>>> ToBatchStreamPageAsync<TKey, TValue>(
        this IQueryable<TValue> source,
        Expression<Func<TValue, TKey>> keySelector,
        PagingArguments arguments,
        IAsyncDisposable? lifetime = null,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => source.ToBatchStreamPageAsync<TKey, TValue, TValue>(
            keySelector,
            null,
            arguments,
            includeTotalCount: arguments.IncludeTotalCount,
            lifetime,
            cancellationToken);

    /// <summary>
    /// Executes a batch query with paging and returns the selected streaming pages for each
    /// parent, from one flat, key-ordered query.
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
    /// <param name="lifetime">
    /// A resource, such as a database context, that must stay alive for as long as any of the
    /// returned pages is being read. It is disposed once every page has completed or been
    /// disposed, or immediately if this call does not end up streaming any rows.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token that governs creating the pages.
    /// </param>
    /// <typeparam name="TKey">
    /// The type of the parent key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the items in the queryable.
    /// </typeparam>
    /// <returns>
    /// A dictionary mapping each requested key to its streaming page of results.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified.
    /// </exception>
    public static ValueTask<Dictionary<TKey, StreamPage<TValue>>> ToBatchStreamPageAsync<TKey, TValue>(
        this IQueryable<TValue> source,
        Expression<Func<TValue, TKey>> keySelector,
        PagingArguments arguments,
        bool includeTotalCount,
        IAsyncDisposable? lifetime = null,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => source.ToBatchStreamPageAsync<TKey, TValue, TValue>(
            keySelector,
            null,
            arguments,
            includeTotalCount,
            lifetime,
            cancellationToken);

    /// <summary>
    /// Executes a batch query with paging and returns the selected streaming pages for each
    /// parent, from one flat, key-ordered query.
    /// </summary>
    /// <param name="source">
    /// The queryable to be paged.
    /// </param>
    /// <param name="keySelector">
    /// A function to select the key of the parent.
    /// </param>
    /// <param name="valueSelector">
    /// A function to select the value of the items in the queryable, or null if the source
    /// element type is directly assignable to the value type.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments.
    /// </param>
    /// <param name="lifetime">
    /// A resource, such as a database context, that must stay alive for as long as any of the
    /// returned pages is being read. It is disposed once every page has completed or been
    /// disposed, or immediately if this call does not end up streaming any rows.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token that governs creating the pages.
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
    /// A dictionary mapping each requested key to its streaming page of results.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified.
    /// </exception>
    public static ValueTask<Dictionary<TKey, StreamPage<TValue>>> ToBatchStreamPageAsync<TKey, TValue, TElement>(
        this IQueryable<TElement> source,
        Expression<Func<TElement, TKey>> keySelector,
        Func<TElement, TValue>? valueSelector,
        PagingArguments arguments,
        IAsyncDisposable? lifetime = null,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => source.ToBatchStreamPageAsync(
            keySelector,
            valueSelector,
            arguments,
            includeTotalCount: arguments.IncludeTotalCount,
            lifetime,
            cancellationToken);

    /// <summary>
    /// Executes a batch query with paging and returns the selected streaming pages for each
    /// parent, from one flat, key-ordered query. The requested key set comes from a top-level
    /// <c>Contains</c> filter over an in-memory collection on <paramref name="source"/>, or from
    /// a distinct-keys query when none is found; a <c>Concat</c> or <c>Union</c> applied above
    /// that filter is the caller's responsibility.
    /// </summary>
    /// <param name="source">
    /// The queryable to be paged.
    /// </param>
    /// <param name="keySelector">
    /// A function to select the key of the parent.
    /// </param>
    /// <param name="valueSelector">
    /// A function to select the value of the items in the queryable, or null if the source
    /// element type is directly assignable to the value type.
    /// </param>
    /// <param name="arguments">
    /// The paging arguments. When neither <c>first</c> nor <c>last</c> is given, a page size of
    /// 10 is used.
    /// </param>
    /// <param name="includeTotalCount">
    /// If set to <c>true</c> the total count will be included in the result.
    /// </param>
    /// <param name="lifetime">
    /// A resource, such as a database context, that must stay alive for as long as any of the
    /// returned pages is being read. It is disposed once every page has completed or been
    /// disposed, or immediately if this call does not end up streaming any rows.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token that governs creating the pages.
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
    /// A dictionary mapping each requested key to its streaming page of results.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the queryable does not have any keys specified, if <c>first</c> or <c>last</c> is
    /// given and not greater than zero, or if neither a value selector is given nor the source
    /// element type is assignable to the value type.
    /// </exception>
    public static async ValueTask<Dictionary<TKey, StreamPage<TValue>>> ToBatchStreamPageAsync<TKey, TValue, TElement>(
        this IQueryable<TElement> source,
        Expression<Func<TElement, TKey>> keySelector,
        Func<TElement, TValue>? valueSelector,
        PagingArguments arguments,
        bool includeTotalCount,
        IAsyncDisposable? lifetime = null,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        CursorKey[] keys;
        BatchStreamQuery<TKey, TElement> composition;
        var requestedCount = 10;

        try
        {
            ArgumentNullException.ThrowIfNull(source);

            if (arguments.Last is not null && arguments.First is not null)
            {
                throw ThrowHelper.PagingArguments_FirstAndLastBothSpecified();
            }

            PagingQueryComposer.ValidateRequestedCount(arguments);

            if (valueSelector is null && !typeof(TValue).IsAssignableFrom(typeof(TElement)))
            {
                throw ThrowHelper.PagingArguments_ValueSelectorRequired();
            }

            // The order and key columns are folded into the source before its own projection is
            // extracted, and the projection is re-applied on each key's sliced window afterward.
            source = QueryHelpers.EnsureOrderPropsAreSelected(source);
            source = QueryHelpers.EnsureGroupPropsAreSelected(source, keySelector);
            var selector = QueryHelpers.ExtractCurrentSelector(source);

            if (selector is not null)
            {
                source = QueryHelpers.RemoveSelector(source);
            }

            keys = PagingQueryComposer.ParseDataSetKeys(source);

            if (keys.Length == 0)
            {
                throw ThrowHelper.Paging_NoOrderByKeys();
            }

            if (arguments.EnableRelativeCursors
                && string.IsNullOrEmpty(arguments.After)
                && string.IsNullOrEmpty(arguments.Before))
            {
                includeTotalCount = true;
            }

            // When neither `first` nor `last` is given, `arguments.First` defaults to 10.
            if (arguments.First is null && arguments.Last is null)
            {
                arguments = arguments with { First = 10 };
            }

            composition = BuildBatchStreamQuery(
                source,
                keySelector,
                arguments,
                keys,
                selector,
                ref requestedCount);

            // An end cursor forces includeTotalCount.
            if (composition.Cursor?.IsEndCursor == true)
            {
                includeTotalCount = true;
            }

            if (!arguments.IncludeItems && !includeTotalCount)
            {
                throw ThrowHelper.PagingArguments_IncludeItemsFalseRequiresTotalCount();
            }
        }
        catch (Exception validationException)
        {
            await ReleaseLifetimeOnFaultAsync(validationException, lifetime).ConfigureAwait(false);

            throw;
        }

        var cursor = composition.Cursor;
        var isBackward = composition.IsBackward;
        var relative = arguments.EnableRelativeCursors;
        var usesRelativeBackwardFlags = relative && cursor?.IsRelative != false;
        var isEndCursor = cursor?.IsEndCursor == true;
        var needsHasMore = isBackward && !isEndCursor && !usesRelativeBackwardFlags && arguments.After is null;

        // The conjunction of whichever after/before predicates are present, the same window the
        // flat query applies per key.
        var windowPredicate = CombinePredicates(composition.AfterPredicate, composition.BeforePredicate);

        // True whenever a relative continuation cursor needs its own per-key empty check: a fresh
        // count already runs, or a backward page bakes its flags and index before any row streams.
        var needsCursorEmptyCheck = relative
            && cursor is { IsEndCursor: false }
            && (includeTotalCount || (isBackward && usesRelativeBackwardFlags));

        // The requested keys are the caller's key set; a key with zero matching rows still gets
        // an empty page.
        IReadOnlyCollection<TKey> requestedKeys;
        TKey[] distinctRequestedKeys;

        try
        {
            if (ContainsKeysExtractor.TryExtract(source.Expression, keySelector, out var containsKeys))
            {
                requestedKeys = containsKeys!;
            }
            else
            {
                var distinctKeysQuery = source.Select(keySelector).Distinct();

                PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(distinctKeysQuery);

                requestedKeys = await distinctKeysQuery.ToListAsync(cancellationToken).ConfigureAwait(false);
            }

            distinctRequestedKeys = requestedKeys.Distinct().ToArray();

            if (distinctRequestedKeys.Any(key => key is null))
            {
                throw ThrowHelper.PagingArguments_NullRequestedKey();
            }
        }
        catch (Exception keysException)
        {
            await ReleaseLifetimeOnFaultAsync(keysException, lifetime).ConfigureAwait(false);

            throw;
        }

        // An empty requested key set has nothing to page.
        if (distinctRequestedKeys.Length == 0)
        {
            await DisposeLifetimeAsync(lifetime).ConfigureAwait(false);

            return new Dictionary<TKey, StreamPage<TValue>>();
        }

        Dictionary<TKey, int>? counts = null;
        Dictionary<TKey, int>? predicateCounts = null;
        var needsPredicateCount = needsHasMore || needsCursorEmptyCheck;

        if (includeTotalCount || needsPredicateCount)
        {
            try
            {
                var countsQuery = BuildBatchStreamCountsExpression(
                    source,
                    keySelector,
                    needsHasMore ? composition.BeforePredicate : needsCursorEmptyCheck ? windowPredicate : null);

                PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(countsQuery);

                counts = [];

                if (needsPredicateCount)
                {
                    predicateCounts = [];
                }

                await foreach (var row in countsQuery.AsAsyncEnumerable()
                    .WithCancellation(cancellationToken)
                    .ConfigureAwait(false))
                {
                    counts[row.Key] = row.Count;

                    if (needsPredicateCount)
                    {
                        predicateCounts![row.Key] = row.PredicateCount;
                    }
                }
            }
            catch (Exception countException)
            {
                await ReleaseLifetimeOnFaultAsync(countException, lifetime).ConfigureAwait(false);

                throw;
            }
        }

        var effectiveValueSelector = valueSelector ?? (static (TElement element) => (TValue)(object)element!);

        // When IncludeItems is false, only the grouped count runs and every requested key
        // resolves to a count-only page.
        if (!arguments.IncludeItems)
        {
            await DisposeLifetimeAsync(lifetime).ConfigureAwait(false);

            var countOnlyMap = new Dictionary<TKey, StreamPage<TValue>>(distinctRequestedKeys.Length);

            foreach (var key in distinctRequestedKeys)
            {
                var keyTotal = counts!.GetValueOrDefault(key);
                var countOnlyIndex = relative
                    ? PagingQueryableExtensions.CreateIndex(arguments, cursor, keyTotal) ?? 1
                    : (int?)null;

                countOnlyMap[key] = await CreateResolvedBatchPageAsync(
                    keys,
                    effectiveValueSelector,
                    hasNextPage: false,
                    hasPreviousPage: false,
                    countOnlyIndex,
                    requestedCount,
                    keyTotal,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            return countOnlyMap;
        }

        IAsyncEnumerator<StreamBatchRow<TKey, TElement>> rowEnumerator;

        try
        {
            PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(composition.FlatQuery);

            rowEnumerator = composition.FlatQuery.AsAsyncEnumerable().GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception buildException)
        {
            await ReleaseLifetimeOnFaultAsync(buildException, lifetime).ConfigureAwait(false);

            throw;
        }

        // A null pump means an empty key set: the enumerator and lifetime are already released.
        var pump = await StreamBatchPump<TKey, TElement>.CreateAsync(
            rowEnumerator,
            distinctRequestedKeys,
            lifetime)
            .ConfigureAwait(false);

        if (pump is null)
        {
            return new Dictionary<TKey, StreamPage<TValue>>();
        }

        // Decides a relative or end-cursor key's empty status up front from the fetched counts,
        // before any row is seen. A plain page's flags resolve dynamically through
        // FlagsFromFirstRow instead.
        bool IsKeyEmpty(TKey key)
        {
            if (isEndCursor)
            {
                // Uses this key's own fresh total, never the cursor's cached one.
                var pagesBeforeLast = -cursor!.Offset!.Value;
                var keyTotal = counts!.GetValueOrDefault(key);

                return (int)Math.Ceiling(keyTotal / (double)requestedCount) - pagesBeforeLast < 1;
            }

            if (!relative)
            {
                return false;
            }

            if (needsCursorEmptyCheck)
            {
                return predicateCounts!.GetValueOrDefault(key) <= composition.SkipAmount;
            }

            if (cursor is null)
            {
                return counts!.GetValueOrDefault(key) <= composition.SkipAmount;
            }

            if (needsHasMore)
            {
                var priorCount = arguments.Before is not null
                    ? predicateCounts!.GetValueOrDefault(key)
                    : counts!.GetValueOrDefault(key);

                return priorCount <= composition.SkipAmount;
            }

            return false;
        }

        var map = new Dictionary<TKey, StreamPage<TValue>>(distinctRequestedKeys.Length);

        foreach (var key in distinctRequestedKeys)
        {
            var totalCount = includeTotalCount ? counts!.GetValueOrDefault(key) : cursor?.TotalCount;
            var definition = IsKeyEmpty(key)
                ? CreateEmptyBatchStreamDefinition<TElement>(requestedCount, isEndCursor, totalCount)
                : CreateBatchStreamDefinition<TElement>(
                    arguments,
                    cursor,
                    relative,
                    isBackward,
                    usesRelativeBackwardFlags,
                    requestedCount,
                    totalCount,
                    // Counts rows before the `before` cursor when one was given; with no incoming
                    // cursor, "before this page" is the key's whole row count.
                    needsHasMore
                        ? arguments.Before is not null
                            ? predicateCounts!.GetValueOrDefault(key)
                            : counts!.GetValueOrDefault(key)
                        : 0,
                    composition.SkipAmount);
            var createCursor = CreateCursorFactory<TElement>(keys, relativeShaped: definition.Index is not null);

            map[key] = pump.CreatePage(key, definition, effectiveValueSelector, createCursor);
        }

        return map;
    }

    // Builds the definition for a key whose slice is known up front to be empty: no index (1 for
    // an end cursor), both flags false, and the key's own total.
    private static StreamPageDefinition<TElement> CreateEmptyBatchStreamDefinition<TElement>(
        int requestedCount,
        bool isEndCursor,
        int? totalCount)
        => new(
            RequestedCount: requestedCount,
            Forward: true,
            TrailingSentinel: false,
            SkipFront: isEndCursor ? requestedCount * 2 : 0,
            SkipFrontFromCount: null,
            Index: isEndCursor ? 1 : null,
            RequestedSize: requestedCount,
            TotalCount: totalCount,
            HasNextPage: false,
            HasPreviousPage: false,
            FlagsFromFirstRow: null);

    // Builds one requested key's page definition for the given arguments and cursor. A plain
    // page's flags that depend on whether a row arrives are supplied through FlagsFromFirstRow.
    private static StreamPageDefinition<TElement> CreateBatchStreamDefinition<TElement>(
        PagingArguments arguments,
        Cursor? cursor,
        bool relative,
        bool isBackward,
        bool usesRelativeBackwardFlags,
        int requestedCount,
        int? totalCount,
        int beforeCount,
        int skipAmount)
    {
        if (cursor?.IsEndCursor == true)
        {
            // Every part of an end-cursor page comes from this key's own fresh total.
            var pagesBeforeLast = -cursor.Offset!.Value;
            var freshTotal = totalCount!.Value;
            var index = (int)Math.Ceiling(freshTotal / (double)requestedCount) - pagesBeforeLast;
            var skipFront = 0;
            var forward = false;
            var trailingSentinel = false;

            if (pagesBeforeLast == 0)
            {
                if (freshTotal > requestedCount && freshTotal % requestedCount != 0)
                {
                    skipFront = requestedCount - freshTotal % requestedCount;
                }
            }
            else
            {
                // The exact page sits at the end of this key's own window, in ascending order.
                var remainder = freshTotal % requestedCount == 0 ? requestedCount : freshTotal % requestedCount;
                var window = Math.Min(requestedCount * 2, freshTotal - (pagesBeforeLast - 1) * requestedCount);
                skipFront = window - remainder - requestedCount;
                forward = true;
                trailingSentinel = true;
            }

            return new StreamPageDefinition<TElement>(
                RequestedCount: requestedCount,
                Forward: forward,
                TrailingSentinel: trailingSentinel,
                SkipFront: skipFront,
                SkipFrontFromCount: null,
                Index: index,
                RequestedSize: requestedCount,
                TotalCount: freshTotal,
                HasNextPage: pagesBeforeLast > 0,
                HasPreviousPage: index > 1,
                FlagsFromFirstRow: null);
        }

        var relativeIndex = relative && totalCount is not null
            ? PagingQueryableExtensions.CreateIndex(arguments, cursor, totalCount) ?? 1
            : (int?)null;

        if (isBackward && !usesRelativeBackwardFlags)
        {
            var hasNext = arguments.Before is not null;
            var hasPrevious = arguments.After is not null || beforeCount > skipAmount + requestedCount;

            return new StreamPageDefinition<TElement>(
                RequestedCount: requestedCount,
                Forward: false,
                TrailingSentinel: false,
                SkipFront: 0,
                SkipFrontFromCount: null,
                Index: relativeIndex,
                RequestedSize: requestedCount,
                TotalCount: totalCount,
                HasNextPage: null,
                HasPreviousPage: null,
                FlagsFromFirstRow: _ => (hasNext, hasPrevious));
        }

        if (isBackward)
        {
            var hasPreviousPage = relativeIndex is > 1;
            var hasNextPage = relativeIndex is not null && totalCount is not null
                && relativeIndex < Math.Ceiling(totalCount.Value / (double)requestedCount);

            return new StreamPageDefinition<TElement>(
                RequestedCount: requestedCount,
                Forward: false,
                TrailingSentinel: false,
                SkipFront: 0,
                SkipFrontFromCount: null,
                Index: relativeIndex,
                RequestedSize: requestedCount,
                TotalCount: totalCount,
                HasNextPage: hasNextPage,
                HasPreviousPage: hasPreviousPage,
                FlagsFromFirstRow: null);
        }

        var hasNextIfBefore = arguments.Before is not null;
        var hasPreviousIfAfter = arguments.After is not null;

        return new StreamPageDefinition<TElement>(
            RequestedCount: requestedCount,
            Forward: true,
            TrailingSentinel: true,
            SkipFront: 0,
            SkipFrontFromCount: null,
            Index: relativeIndex,
            RequestedSize: requestedCount,
            TotalCount: totalCount,
            HasNextPage: null,
            HasPreviousPage: null,
            FlagsFromFirstRow: _ => (hasNextIfBefore ? true : null, hasPreviousIfAfter));
    }

    private static async ValueTask<StreamPage<TValue>> CreateResolvedBatchPageAsync<TValue, TElement>(
        CursorKey[] keys,
        Func<TElement, TValue> valueSelector,
        bool hasNextPage,
        bool hasPreviousPage,
        int? index,
        int requestedCount,
        int? totalCount,
        CancellationToken cancellationToken)
    {
        var definition = new StreamPageDefinition<TElement>(
            RequestedCount: requestedCount,
            Forward: true,
            TrailingSentinel: false,
            SkipFront: 0,
            SkipFrontFromCount: null,
            Index: index,
            RequestedSize: requestedCount,
            TotalCount: totalCount,
            HasNextPage: hasNextPage,
            HasPreviousPage: hasPreviousPage,
            FlagsFromFirstRow: null);
        var createCursor = CreateCursorFactory<TElement>(keys, relativeShaped: index is not null);

        return await ElementCursorStreamPage<TElement, TValue>.CreatePrimedAsync(
            pump: null,
            definition,
            valueSelector,
            createCursor,
            cancellationToken)
            .ConfigureAwait(false);
    }
}
