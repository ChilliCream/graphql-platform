using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Caching;

internal sealed class QueryCacheMiddleware
{
    private readonly ICacheControlOptions _options;
    private readonly RequestDelegate _next;

    private QueryCacheMiddleware(
        RequestDelegate next,
        [SchemaService] ICacheControlOptionsAccessor optionsAccessor)
    {
        _next = next;
        _options = optionsAccessor.CacheControl;
    }

    public async ValueTask InvokeAsync(RequestContext context)
    {
        await _next(context).ConfigureAwait(false);

        if (!_options.Enable || context.ContextData.ContainsKey(SkipQueryCaching))
        {
            // If query caching is disabled or skipped,
            // we do not attempt to cache the result.
            return;
        }

        if (!context.TryGetOperation(out var operation)
            || !operation.ContextData.TryGetValue(ExecutionContextData.CacheControlHeaderValue, out var value)
            || value is not CacheControlHeaderValue cacheControlHeaderValue)
        {
            return;
        }

        // only single operation results can be cached.
        var operationResult = context.Result?.ExpectOperationResult();

        if (operationResult is { Errors: null })
        {
            var contextData =
                operationResult.ContextData is not null
                    ? new ExtensionData(operationResult.ContextData)
                    : [];

            contextData.Add(ExecutionContextData.CacheControlHeaderValue, cacheControlHeaderValue);

            if (operation.ContextData.TryGetValue(ExecutionContextData.VaryHeaderValue, out var varyValue)
                && varyValue is string varyHeaderValue
                && !string.IsNullOrEmpty(varyHeaderValue))
            {
                contextData.Add(ExecutionContextData.VaryHeaderValue, varyHeaderValue);
            }

            context.Result = operationResult.WithContextData(contextData);
        }
    }

    internal static RequestMiddlewareConfiguration Create()
        => new RequestMiddlewareConfiguration(
            (core, next) =>
            {
                var options = core.SchemaServices.GetRequiredService<ICacheControlOptionsAccessor>();
                var middleware = new QueryCacheMiddleware(next, options);
                return context => middleware.InvokeAsync(context);
            },
            nameof(QueryCacheMiddleware));
}
