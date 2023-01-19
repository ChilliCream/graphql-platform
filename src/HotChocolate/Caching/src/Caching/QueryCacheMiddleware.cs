using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using static HotChocolate.Caching.WellKnownContextData;

namespace HotChocolate.Caching;

internal sealed class QueryCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly QueryCache[] _caches;
    private readonly ICacheControlOptions _options;

    public QueryCacheMiddleware(
        RequestDelegate next,
        [SchemaService] IEnumerable<QueryCache> caches,
        [SchemaService] ICacheControlOptionsAccessor optionsAccessor)
    {
        _next = next;
        _caches = caches.ToArray();
        _options = optionsAccessor.CacheControl;
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        await _next(context).ConfigureAwait(false);

        if (!_options.Enable || context.ContextData.ContainsKey(SkipQueryCaching))
        {
            // If query caching is disabled or skipped,
            // we do not attempt to cache the result.
            return;
        }

        if (!CanOperationResultBeCached(context))
        {
            return;
        }

        if (!(context.Operation?.ContextData.TryGetValue(
            WellKnownContextData.CacheControlConstraints,
            out var value) ?? false) ||
            value is not CacheControlConstraints constraints)
        {
            return;
        }

        if (constraints.MaxAge is null)
        {
            // No field in the query specified a maxAge value,
            // so we do not attempt to cache it.
            return;
        }

        for (var i = 0; i < _caches.Length; i++)
        {
            var cache = _caches[i];

            if (!cache.ShouldWriteQueryResultToCache(context))
            {
                continue;
            }

            await cache.WriteQueryResultToCacheAsync(context, constraints, _options);
        }
    }

    private static bool CanOperationResultBeCached(IRequestContext context)
    {
        if (context.Result is not IQueryResult queryResult)
        {
            // Result is potentially deferred or batched,
            // we can not cache the entire query.
            return false;
        }

        if (context.Operation?.Definition.Operation is not OperationType.Query)
        {
            // Request is not a query, so we do not cache it.
            return false;
        }

        if (queryResult.Errors is { Count: > 0 })
        {
            // Result has unexpected errors, we do not want to cache it.
            return false;
        }

        return true;
    }
}
