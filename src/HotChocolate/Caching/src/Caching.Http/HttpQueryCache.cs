using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Caching.Http;

internal sealed class HttpQueryCache : DefaultQueryCache
{
    private static readonly string _httpContextKey = nameof(HttpContext);
    private const string _cacheControlValueTemplate = "{0}, max-age={1}";

    public override bool ShouldReadResultFromCache(IRequestContext context)
    {
        // The cache request is supposed to be handled by a CDN 
        // or another inbetween HTTP caching layer.
        // We do not know how to resolve the query from cache,
        // if we actually get here, so we bail.

        return false;
    }

    public override Task<IQueryResult?> TryReadCachedQueryResultAsync(
        IRequestContext context, IQueryCacheSettings settings)
    {
        return Task.FromResult<IQueryResult?>(null);
    }

    public override Task CacheQueryResultAsync(IRequestContext context,
        QueryCacheResult result, IQueryCacheSettings settings)
    {
        if (!context.ContextData.TryGetValue(_httpContextKey, out var httpContextValue)
            || httpContextValue is not HttpContext httpContext)
        {
            return Task.CompletedTask;
        }

        var cacheType = result.Scope switch
        {
            CacheControlScope.Private => "private",
            CacheControlScope.Public => "public",
            _ => throw new Exception("TODO")
        };

        var headerValue = string.Format(_cacheControlValueTemplate,
            cacheType, result.MaxAge);

#if NET6_0_OR_GREATER
        httpContext.Response.Headers.CacheControl = headerValue;
#else
        httpContext.Response.Headers.Add("Cache-Control", headerValue);
#endif

        return Task.CompletedTask;
    }
}