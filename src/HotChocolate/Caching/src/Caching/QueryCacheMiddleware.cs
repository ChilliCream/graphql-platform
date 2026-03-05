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
            || !operation.Features.TryGet<CacheControlHeaderValue>(out var headerValue)
            || !operation.Features.TryGet<ImmutableCacheConstraints>(out var constraints))
        {
            return;
        }

        if (context.Result is OperationResult { Errors.Count: 0, ContextData: { } contextData } operationResult)
        {
            contextData = contextData.Add(ExecutionContextData.CacheControlHeaderValue, headerValue);

            if (constraints.Vary.Length > 0)
            {
                contextData = contextData.Add(ExecutionContextData.VaryHeaderValue, constraints.VaryString);
            }

            operationResult.ContextData = contextData;
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
            WellKnownRequestMiddleware.QueryCacheMiddleware);
}
