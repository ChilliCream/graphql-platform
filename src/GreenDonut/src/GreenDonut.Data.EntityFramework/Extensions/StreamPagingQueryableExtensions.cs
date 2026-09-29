using GreenDonut.Data.Cursors;
using GreenDonut.Data.Expressions;
using GreenDonut.Data.Internal;
using Microsoft.EntityFrameworkCore;

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
    /// If the queryable does not have any keys specified.
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
    /// If the queryable does not have any keys specified, or if
    /// <see cref="PagingArguments.IncludeItems"/> is <c>false</c> while
    /// <paramref name="includeTotalCount"/> is also <c>false</c>.
    /// </exception>
    public static async ValueTask<StreamPage<T>> ToStreamPageAsync<T>(
        this IQueryable<T> source,
        PagingArguments arguments,
        bool includeTotalCount,
        IAsyncDisposable? lifetime = null,
        CancellationToken cancellationToken = default)
    {
        var composition = PagingQueryComposer.Compose(source, arguments, includeTotalCount);
        var originalQuery = composition.OriginalQuery;
        var keys = composition.Keys;
        var cursor = composition.Cursor;
        var requestedCount = composition.RequestedCount;
        var isBackward = composition.IsBackward;
        arguments = composition.Arguments;
        includeTotalCount = composition.IncludeTotalCount;
        var relative = arguments.EnableRelativeCursors;

        // count-only: no row query ever runs, so no lifetime is ever handed to a page.
        if (!arguments.IncludeItems)
        {
            if (!includeTotalCount)
            {
                await DisposeLifetimeAsync(lifetime).ConfigureAwait(false);

                throw new ArgumentException(
                    "IncludeItems can only be false when the total count is included.",
                    nameof(arguments));
            }

            PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(originalQuery);
            var countOnlyTotal = await originalQuery.CountAsync(cancellationToken).ConfigureAwait(false);
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
                var freshCount = await originalQuery.CountAsync(cancellationToken).ConfigureAwait(false);
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
        var needsHasMore = isBackward && !isEndCursor && !relative;

        IQueryable<T> pageQuery;
        IQueryable<T>? hasMoreSource = null;

        if (isBackward)
        {
            // Backward pages take exactly the requested count from the inverted order and re-apply the original order.
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

        var rowQuery = BuildRowQuery(pageQuery, originalQuery, hasMoreSource, includeTotalCount);

        PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(rowQuery);

        var enumerator = rowQuery.AsAsyncEnumerable().GetAsyncEnumerator(cancellationToken);
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
            // An empty row query carries no inlined count; a requested count is fetched separately.
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

            var emptyTotalCount = composition.TotalCount;

            if (includeTotalCount)
            {
                PagingQueryableExtensions.TryGetQueryInterceptor()?.OnBeforeExecute(originalQuery);
                emptyTotalCount = await originalQuery.CountAsync(cancellationToken).ConfigureAwait(false);
            }

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
        else if (isBackward && !relative)
        {
            index = null;
            hasNextPage = arguments.Before is not null;
            hasPreviousPage = firstRow.HasMore ?? false;
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
}
