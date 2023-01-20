using System.Threading.Tasks;
using HotChocolate.Execution;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Caching;

internal sealed class QueryCacheMiddleware
{
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

        if (context.Operation?.ContextData is null ||
            !context.Operation.ContextData.TryGetValue(CacheControlHeaderValue, out var value) ||
            value is not string cacheControlHeaderValue)
        {
            return;
        }

        var queryResult = context.Result?.ExpectQueryResult();

        if (queryResult is not null)
        {
            var contextData =
                queryResult.ContextData is not null
                    ? new ExtensionData(queryResult.ContextData)
                    : new ExtensionData();

            contextData.Add(CacheControlHeaderValue, cacheControlHeaderValue);

            context.Result = new QueryResult(
                queryResult.Data,
                queryResult.Errors,
                queryResult.Extensions,
                contextData,
                queryResult.Items,
                queryResult.Incremental,
                queryResult.Label,
                queryResult.Path,
                queryResult.HasNext);
        }
    }
}
