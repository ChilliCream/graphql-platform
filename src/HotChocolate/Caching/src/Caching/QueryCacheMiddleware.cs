using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Caching;

internal sealed class QueryCacheMiddleware
{
    private const string _cacheControlValueTemplate = "{0}, max-age={1}";
    private const string _cacheControlPrivateScope = "private";
    private const string _cacheControlPublicScope = "public";


    private readonly RequestDelegate _next;
    private readonly ICacheControlOptions _options;

    public QueryCacheMiddleware(
        RequestDelegate next,
        [SchemaService] ICacheControlOptionsAccessor optionsAccessor)
    {
        _next = next;
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
            value is not ICacheConstraints constraints)
        {
            return;
        }

        var cacheType = constraints.Scope switch
        {
            CacheControlScope.Private => _cacheControlPrivateScope,
            CacheControlScope.Public => _cacheControlPublicScope,
            _ => throw ThrowHelper.UnexpectedCacheControlScopeValue(constraints.Scope)
        };

        var headerValue = string.Format(_cacheControlValueTemplate, cacheType, constraints.MaxAge);
        var queryResult = context.Result?.ExpectQueryResult();

        if (queryResult is not null)
        {
            context.Result = QueryResultBuilder.FromResult(queryResult)
                .SetContextData(CacheControlHeaderValue, headerValue)
                .Create();
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
