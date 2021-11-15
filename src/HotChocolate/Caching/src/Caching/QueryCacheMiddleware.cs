using System;
using System.Threading.Tasks;
using HotChocolate.Execution;

namespace HotChocolate.Caching;

public sealed class QueryCacheMiddleware
{
    private static readonly string _contextKey = nameof(QueryCacheSettings);

    private readonly RequestDelegate _next;
    private readonly IQueryCache _cache;

    public QueryCacheMiddleware(RequestDelegate next, IQueryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async ValueTask InvokeAsync(IRequestContext context)
    {
        IQueryCacheSettings settings;

        if (context.ContextData.TryGetValue(_contextKey, out var untypedSettings) &&
            untypedSettings is QueryCacheSettings typedSettings)
        {
            settings = typedSettings;
        }
        else
        {
            settings = new DefaultQueryCacheSettings();
        }

        if (settings.Enable && _cache.ShouldReadResultFromCache(context))
        {
            IQueryResult? cachedResult =
                await _cache.TryReadCachedQueryResultAsync(context, settings);

            if (cachedResult is not null)
            {
                // todo: return result served from cache
            }
        }

        await _next(context).ConfigureAwait(false);

        if (settings.Enable && _cache.ShouldWriteResultToCache(context))
        {
            // todo: compute
            var result = new QueryCacheResult();

            await _cache.CacheQueryResultAsync(context, result, settings);
        }
    }

    private class DefaultQueryCacheSettings : IQueryCacheSettings
    {
        public bool Enable { get; } = false;
        public int DefaultMaxAge { get; } = 0;
        public GetSessionIdDelegate? GetSessionId { get; } = null;
    }
}