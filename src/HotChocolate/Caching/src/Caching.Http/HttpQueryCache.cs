using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Caching.Http;

public class HttpQueryCache : QueryCache
{
    private const string _httpContextKey = nameof(HttpContext);
    private const string _cacheControlValueTemplate = "{0}, max-age={1}";
    private const string _cacheControlPrivateScope = "private";
    private const string _cacheControlPublicScope = "public";

    public override ValueTask WriteQueryToCacheAsync(IRequestContext context,
        ICacheControlResult result, ICacheControlOptions options)
    {
        if (!context.ContextData.TryGetValue(_httpContextKey, out var httpContextValue)
            || httpContextValue is not HttpContext httpContext)
        {
            return ValueTask.CompletedTask;
        }

        var cacheType = result.Scope switch
        {
            CacheControlScope.Private => _cacheControlPrivateScope,
            CacheControlScope.Public => _cacheControlPublicScope,
            _ => throw ThrowHelper.UnexpectedCacheControlScopeValue(result.Scope)
        };

        var headerValue = string.Format(_cacheControlValueTemplate,
            cacheType, result.MaxAge);

#if NET6_0_OR_GREATER
        httpContext.Response.Headers.CacheControl = headerValue;
#else
        httpContext.Response.Headers.Add(HeaderNames.CacheControl, headerValue);
#endif

        return ValueTask.CompletedTask;
    }
}
